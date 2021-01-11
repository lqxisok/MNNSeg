#ifndef SEGMNN_SKY_H
#define SEGMNN_SKY_H

#include <iostream>
#include <string>
#include <algorithm>
#include <vector>
#include <memory>
#include <expr/Expr.hpp>
#include <expr/ExprCreator.hpp>
#include <opencv2/opencv.hpp>

#include "Interpreter.hpp"
#include "MNNDefine.h"
#include "Tensor.hpp"
#include "ImageProcess.hpp"



#define ERR_CODE int

namespace segmnnsky
{
    class pcnetMNN
    {
    private:
        std::shared_ptr<MNN::Interpreter> interpreter;
        MNN::ScheduleConfig config;
        MNN::BackendConfig backendConfig;
        MNN::Session *session = nullptr;
        MNN::Tensor *input_tensor;
        MNN::CV::ImageProcess::Config imgConfig;
        std::shared_ptr<MNN::CV::ImageProcess> pretreat = nullptr;
        int w = 1280;
        int h = 720;

    public:
        pcnetMNN();
        ~pcnetMNN();
        ERR_CODE initInterpreter(std::string modelPath, int numThread, int height, int width);
        ERR_CODE runSession();
        // overload the process Image with a path and a Mat
        ERR_CODE processImage(std::string imagePath);
        ERR_CODE processImage(cv::Mat image);
        cv::Mat getOutput();
        ERR_CODE releaseSession();
        void setH(int height) {h = height;};
        void setW(int width) {w = width;};
        int getH(){return h;};
        int getW(){return w;};
    };
    
    
} // namespace segmnnsky


#endif // SEGMNN_SKY
