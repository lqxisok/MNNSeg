#include "export.h"
#include "segmnn.hpp"

#define STB_IMAGE_IMPLEMENTATION
#define STB_IMAGE_WRITE_IMPLEMENTATION
#include "stb_image.h"
#include "stb_image_write.h"

using std::string;


int main(int argc, char **argv){

    initializeModel(4, 720, 1280);

    // Process the image from a (void *) pointer
    int width, height, channel;
    string path = "/root/github/MNNSeg/test.jpg";
    string out_path = "/root/github/MNNSeg/testa.png";
    auto inputImage = stbi_load(path.c_str(), &width, &height, &channel, 3);


    processImage((void*)inputImage);
    runSession();
    uchar * out =  (uchar *)getOutput();

    // use the stbi to write the image
    stbi_write_png(out_path.c_str(), width, height, 1, out, 1 * width);

    // I have test the output. It only contains 0 and 100 (the seg label is multiplied by 100 for visualization).
    // please change the file  `src/segmnn.cpp -> getOutput() -> result.at<uchar>(i, j) = index * 100;`
    releaseSession();

    std::cout << "done" << std::endl;
    return 0;
}