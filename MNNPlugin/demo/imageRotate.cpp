#include <iostream>
#include <expr/Expr.h>
#include <expr/ExprCreator.h>

#include "Interpreter.h"
#include "MNNDefine.h"
#include "Tensor.h"
#include "ImageProcess.h"

#define STB_IMAGE_IMPLEMENTATION
#define STB_IMAGE_WRITE_IMPLEMENTATION
#include "stb_image.h"
#include "stb_image_write.h"


int main(int argc, char const *argv[])
{
    // test the image process api
    auto inputPath = "/root/github/MNNSeg/test.jpg";
    auto outPath = "/root/github/MNNSeg/test_rotate.jpg";
    auto outMemPath = "/root/github/MNNSeg/test_Mem.jpg";

    int width, height, channel;
    auto inputImage = stbi_load(inputPath, &width, &height, &channel, 4);
    int length = width * height * channel;
    int memW, memH, memC;
    auto memImage = stbi_load_from_memory(inputImage, length, &memW, &memH, &memC, 3);

    std::cout << " Size : " << width << " - " << height <<  " - " << channel << std::endl;
    std::cout << " Size : " << memW << " - " << memH <<  " - " << memC << std::endl;


    MNN::CV::Matrix trans;
    trans.setScale(1.0 / (width - 1), 1.0 / (height - 1));
    trans.postRotate(30.0, 0.5, 0.5);
    trans.postScale((width - 1), (height - 1));

    MNN::CV::ImageProcess::Config imConfig;
    imConfig.filterType   = MNN::CV::NEAREST;
    imConfig.sourceFormat = MNN::CV::RGBA;
    imConfig.destFormat   = MNN::CV::RGBA;
    imConfig.wrap         = MNN::CV::ZERO;
    std::shared_ptr<MNN::CV::ImageProcess> pretreat(MNN::CV::ImageProcess::create(imConfig));
    pretreat->setMatrix(trans);
        {
        std::shared_ptr<MNN::Tensor> wrapTensor(MNN::CV::ImageProcess::createImageTensor<uint8_t>(width, height, 4, nullptr));
        pretreat->convert((uint8_t*)inputImage, width, height, 0, wrapTensor.get());
        stbi_write_png(outPath, width, height, 4, wrapTensor->host<uint8_t>(), 4 * width);
    }
    stbi_write_png(outMemPath, width, height, 4, memImage, 4 * width);
    stbi_image_free(memImage);
    //stbi_image_free(inputImage);
    return 0;
}
