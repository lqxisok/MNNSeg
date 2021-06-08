#include "segmnn.hpp"

MNN::ErrorCode segmnnsky::pcnetMNN::initInterpreter(std::string modelPath, int numThread, int width, int height, int channel){

    // config
    config.numThread = numThread;
    backendConfig.precision = (MNN::BackendConfig::PrecisionMode) 2;
    config.backendConfig = &backendConfig;
    // interpreter
    interpreter = std::shared_ptr<MNN::Interpreter>(MNN::Interpreter::createFromFile(modelPath.c_str()));
    if (nullptr == interpreter) return MNN::INVALID_VALUE;
    // session
    session = interpreter->createSession(config);
    // input tensor
    input_tensor = interpreter->getSessionInput(session, nullptr);
    // set h and w
    setH(height);
    setW(width);
    setC(channel);
    
    // resize Session
    interpreter->resizeTensor(input_tensor, {1, c, h, w});
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
    "Inference Width: " << getW() << "\n" <<
    "Inference Channel: " << getC() << "\n" <<  std::endl;
    return MNN::NO_ERROR;
} 

MNN::ErrorCode segmnnsky::pcnetMNN::runSession(){
    return interpreter->runSession(session);
}

MNN::ErrorCode segmnnsky::pcnetMNN::processImage(uchar * data){
    /*
        stride need set to the `mat.step[0]`.
        `mat.step[0]` means w*c
    */
    int stride = w * c;
    if (nullptr == pretreat) return MNN::INVALID_VALUE;
    pretreat->convert(data, w, h, stride, input_tensor);
    return MNN::NO_ERROR;
}

MNN::ErrorCode segmnnsky::pcnetMNN::releaseSession(){
    interpreter->releaseModel();
    interpreter->releaseSession(session);

    return MNN::NO_ERROR;
}


MNN::ErrorCode segmnnsky::pcnetMNN::getOutput(uchar* outputArray){
    // get the output
    auto outputTensor = interpreter->getSessionOutput(session, "output1");
    auto nchwTensor = new MNN::Tensor(outputTensor, MNN::Tensor::CAFFE);
    outputTensor->copyToHostTensor(nchwTensor);

    auto mask = outputTensor->host<float>()[0];


    auto dims = nchwTensor->shape();
    auto height_out = dims[2];
    auto width_out = dims[3];
    auto channel_out = dims[1];

    std::cout << height_out << ' ' << width_out << ' ' << channel_out << std::endl;
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
            outputArray[i * width_out + j] = index * 100;
        }
    }
    delete nchwTensor;
    return MNN::NO_ERROR;
}

segmnnsky::pcnetMNN::pcnetMNN()
{
    std::cout << "Hello Seg" << std::endl; 
}

segmnnsky::pcnetMNN::~pcnetMNN()
{
}