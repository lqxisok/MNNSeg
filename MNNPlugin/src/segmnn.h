#ifndef SEGMNN_SKY_H
#define SEGMNN_SKY_H

#include <iostream>
#include <string>
#include <algorithm>
#include <vector>
#include <memory>
#include <expr/Expr.h>
#include <expr/ExprCreator.h>

#include "Interpreter.h"
#include "MNNDefine.h"
#include "Tensor.h"
#include "ImageProcess.h"

typedef unsigned char uchar;

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
        int c = 3;
        //uchar* outputArray = nullptr;
        //uchar outputArray[720][1280];

    public:
        pcnetMNN();
        ~pcnetMNN();
        MNN::ErrorCode initInterpreter(std::string modelPath, int numThread, int width, int height, int channel);
        MNN::ErrorCode runSession();
        MNN::ErrorCode processImage(uchar * data);
        MNN::ErrorCode getOutput(uchar * outputArray);
        MNN::ErrorCode releaseSession();
        void setH(int height) {h = height;};
        void setW(int width) {w = width;};
        void setC(int channel) {c = channel;};
        int getH(){return h;};
        int getW(){return w;};
        int getC(){return c;};
        //uchar * getOutPtr(){return outputArray;};
    };
    
    
} // namespace segmnnsky


#endif // SEGMNN_SKY
