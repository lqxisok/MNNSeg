#include <iostream>
#include "Interpreter.hpp"
#include <string>
#include "MNNDefine.h"
#include "Tensor.hpp"
#include "ImageProcess.hpp"
#include <algorithm>
#include <iostream>
#include <string>
#include <vector>
#include <memory>
#include <chrono>
#include <expr/Expr.hpp>
#include <expr/ExprCreator.hpp>
#include <opencv2/opencv.hpp>
using cv::imread;
using std::string;
using cv::Mat;
using namespace cv;
using namespace MNN;
using namespace MNN::CV;
using namespace MNN::Express;

int main(int argc, char **argv){
    if (argc <= 2) {
        fprintf(stderr, "Usage: %s <model-name>.mnn <image file>]\n", argv[0]);
        return 1;
    }

    // Model
    string mnn_path = argv[1];
    std::shared_ptr<MNN::Interpreter> interpreter;
    interpreter = std::shared_ptr<MNN::Interpreter>(MNN::Interpreter::createFromFile(argv[1]));

    // config
    MNN::ScheduleConfig config;
    config.numThread = 4;
    MNN::BackendConfig backendConfig;
    backendConfig.precision = (MNN::BackendConfig::PrecisionMode) 2;
    config.backendConfig = &backendConfig;

    // Session
    MNN::Session *session = nullptr;
    session = interpreter->createSession(config);

    // input tensor
    MNN::Tensor *input_tensor = nullptr;
    input_tensor = interpreter->getSessionInput(session, nullptr);
    
    // Image
    string img_name = argv[2];
    Mat img = imread(img_name);
    Mat image;
    int w = 1280;
    int h = 720;
    int image_h = img.rows;
    int image_w = img.cols;
    cv::resize(img, image, cv::Size(w, h));


    // Resize Session
    interpreter->resizeTensor(input_tensor, {1, 3, h, w});
    interpreter->resizeSession(session);

    // Process Input
    MNN::CV::ImageProcess::Config imgConfig;
    imgConfig.filterType = MNN::CV::BILINEAR;
    // float mean[3] = {123.68f, 116.78f, 103.94f};
    // float normals[3] = {0.017f,0.017f,0.017f};
    float mean[3] = {0.0f, 0.0f, 0.0f};
    float normals[3] = {1.0f,1.0f,1.0f};

    ::memcpy(imgConfig.mean, mean, sizeof(mean));
    ::memcpy(imgConfig.normal, normals, sizeof(normals));
    imgConfig.sourceFormat = MNN::CV::BGR;
    imgConfig.destFormat = MNN::CV::RGB;

    std::shared_ptr<MNN::CV::ImageProcess> pretreat(
        MNN::CV::ImageProcess::create(imgConfig)
    );
    pretreat->convert(image.data, w, h, image.step[0], input_tensor);

    // timer
    auto start = std::chrono::steady_clock::now();

    // run network
    interpreter->runSession(session);

    // timer
    auto end = std::chrono::steady_clock::now();
    auto elapsed = std::chrono::duration_cast<std::chrono::microseconds>(end - start);
    std::cout << "Using " << double(elapsed.count()) * std::chrono::microseconds::period::num / std::chrono::microseconds::period::den << "s." << std::endl;

    // get the output
    auto outputTensor = interpreter->getSessionOutput(session, "output1");
    auto nchwTensor = new MNN::Tensor(outputTensor, MNN::Tensor::CAFFE);
    outputTensor->copyToHostTensor(nchwTensor);


    auto dims = nchwTensor->shape();
    auto height_out = dims[2];
    auto width_out = dims[3];
    auto channel_out = dims[1];

    Mat result(height_out, width_out, CV_8U, Scalar(0));
    for (int i = 0; i < height_out; i++)
    {
        for (int j = 0; j < width_out; j++)
        {   
            int index = 0;
            float maxvalue=0.0;
            for (int c = 0; c < channel_out; c++)
            {   
                if (c == 0)
                {
                    maxvalue = nchwTensor->host<float>()[i*width_out + j + c*height_out*width_out];
                } else if(nchwTensor->host<float>()[i*width_out + j + c*height_out*width_out] > maxvalue){
                    index = c;
                }
                
            }
            
            result.at<uchar>(i, j) = index * 100;
        }
        
    }
    imwrite("./mnnout.png", result);

    // Release the interpreter
    interpreter->releaseModel();
    interpreter->releaseSession(session);



    std::cout << "done" << std::endl;
    return 0;
}