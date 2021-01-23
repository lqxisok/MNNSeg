#ifndef SEGMNN_SKY_H
#define SEGMNN_SKY_H

#include <iostream>
#include <string>
#include <algorithm>
#include <vector>
#include <memory>
#include <expr/Expr.hpp>
#include <expr/ExprCreator.hpp>

#include "Interpreter.hpp"
#include "MNNDefine.h"
#include "Tensor.hpp"
#include "ImageProcess.hpp"

typedef unsigned char uchar;
typedef int ERR_CODE;

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
        uchar outputArray[720][1280];

    public:
        pcnetMNN();
        ~pcnetMNN();
        ERR_CODE initInterpreter(std::string modelPath, int numThread, int height, int width);
        ERR_CODE runSession();
        ERR_CODE processImage(void * data);
        ERR_CODE getOutput();
        ERR_CODE releaseSession();
        void setH(int height) {h = height;};
        void setW(int width) {w = width;};
        void setC(int channel) {c = channel;};
        int getH(){return h;};
        int getW(){return w;};
        int getC(){return c;};
        void * getOutPtr(){return (void *) outputArray;};
    };
    
    
} // namespace segmnnsky


#endif // SEGMNN_SKY
