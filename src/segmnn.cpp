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
    return 1;
} 

ERR_CODE segmnnsky::pcnetMNN::runSession(){
    interpreter->runSession(session);
    return 1;
}

ERR_CODE segmnnsky::pcnetMNN::processImage(void * data){
    /*
        stride need set to the `mat.step[0]`.
        `mat.step[0]` means w*c
    */
    int stride = w * c;
    pretreat->convert((uchar *)data, w, h, stride, input_tensor);
    return 1;
}

ERR_CODE segmnnsky::pcnetMNN::releaseSession(){
    interpreter->releaseModel();
    interpreter->releaseSession(session);
    return 1;
}


ERR_CODE segmnnsky::pcnetMNN::getOutput(){
    // get the output
    auto outputTensor = interpreter->getSessionOutput(session, "output1");
    auto nchwTensor = new MNN::Tensor(outputTensor, MNN::Tensor::CAFFE);
    outputTensor->copyToHostTensor(nchwTensor);

    auto mask = outputTensor->host<float>()[0];


    auto dims = nchwTensor->shape();
    auto height_out = dims[2];
    auto width_out = dims[3];
    auto channel_out = dims[1];

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
            outputArray[i][j] = index * 100;
        }
        
    }
    return 1;
}

segmnnsky::pcnetMNN::pcnetMNN()
{
    std::cout << "Hello Seg" << std::endl; 
}

segmnnsky::pcnetMNN::~pcnetMNN()
{
}