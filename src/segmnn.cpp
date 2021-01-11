#include "segmnn.hpp"

ERR_CODE segmnnsky::pcnetMNN::initInterpreter(std::string modelPath, int numThread, int height, int width){

    // config
    config.numThread = numThread;
    backendConfig.precision = (MNN::BackendConfig::PrecisionMode) 2;
    config.backendConfig = &backendConfig;
    // interpreter
    interpreter = std::shared_ptr<MNN::Interpreter>(MNN::Interpreter::createFromFile(modelPath.c_str()));
    // session
    session = interpreter->createSession(config);
    // input tensor
    input_tensor = interpreter->getSessionInput(session, nullptr);
    // set h and w
    setH(height);
    setW(width);
    
    // resize Session
    interpreter->resizeTensor(input_tensor, {1, 3, h, w});
    interpreter->resizeSession(session);
    // Image Config
    imgConfig.filterType = MNN::CV::BILINEAR;
    float mean[3] = {0.0f, 0.0f, 0.0f};
    float normals[3] = {1.0f,1.0f,1.0f};

    ::memcpy(imgConfig.mean, mean, sizeof(mean));
    ::memcpy(imgConfig.normal, normals, sizeof(normals));
    imgConfig.sourceFormat = MNN::CV::BGR;
    imgConfig.destFormat = MNN::CV::RGB;
    pretreat = std::shared_ptr<MNN::CV::ImageProcess>(MNN::CV::ImageProcess::create(imgConfig));

    // print the info
    std::cout << "Initialization Infos: \n" << 
    "Model Path : " << modelPath << "\n" <<
    "Inference Height: " << getH() << "\n" << 
    "Inference Width: " << getW() << "\n" << std::endl;
    return 1;
} 

ERR_CODE segmnnsky::pcnetMNN::runSession(){
    interpreter->runSession(session);
    return 1;
}

ERR_CODE segmnnsky::pcnetMNN::processImage(std::string imagePath){
    cv::Mat img = cv::imread(imagePath);
    if (img.empty()){
        std::cout << "Could not load image ... \n" << std::endl;
        return -1;
    }
    cv::resize(img, img, cv::Size(w, h));
    pretreat->convert(img.data, w, h, img.step[0], input_tensor);
    return 1;
}

ERR_CODE segmnnsky::pcnetMNN::processImage(cv::Mat image){
    cv::resize(image, image, cv::Size(w, h));
    pretreat->convert(image.data, w, h, image.step[0], input_tensor);
    return 1;
}

ERR_CODE segmnnsky::pcnetMNN::releaseSession(){
    interpreter->releaseModel();
    interpreter->releaseSession(session);
    return 1;
}

cv::Mat segmnnsky::pcnetMNN::getOutput(){
    // get the output
    auto outputTensor = interpreter->getSessionOutput(session, "output1");
    auto nchwTensor = new MNN::Tensor(outputTensor, MNN::Tensor::CAFFE);
    outputTensor->copyToHostTensor(nchwTensor);


    auto dims = nchwTensor->shape();
    auto height_out = dims[2];
    auto width_out = dims[3];
    auto channel_out = dims[1];

    cv::Mat result(height_out, width_out, CV_8U, cv::Scalar(0));
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
    return result;
}

segmnnsky::pcnetMNN::pcnetMNN()
{
    std::cout << "Hello Seg" << std::endl; 
}

segmnnsky::pcnetMNN::~pcnetMNN()
{
}