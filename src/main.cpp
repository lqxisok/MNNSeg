#include "export.h"
#include "segmnn.hpp"

#define STB_IMAGE_IMPLEMENTATION
#define STB_IMAGE_WRITE_IMPLEMENTATION
#include "stb_image.h"
#include "stb_image_write.h"
#include <direct.h>

using std::string;

int main(int argc, char **argv){

    // Process the image from a (void *) pointer
    int width, height, channel;
    string path = "../test.jpg";
    string out_path = "../testa.png";
    auto inputImage = stbi_load(path.c_str(), &width, &height, &channel, 3);

    //string path = getcwd(NULL, 0);
    if (initializeModel("../pcnet.mnn", 4, width, height, channel) == 0)
    {
        processImage(inputImage);
        runSession();

        uchar * out = new uchar[width * height];
        getOutput(out);

        // use the stbi to write the image
        stbi_write_png(out_path.c_str(), width, height, 1, out, 1 * width);

        // I have test the output. It only contains 0 and 100 (the seg label is multiplied by 100 for visualization).
        // please change the file  `src/segmnn.cpp -> getOutput() -> result.at<uchar>(i, j) = index * 100;`
        releaseSession();

        delete(out);

        std::cout << "done" << std::endl;
    }
    else
        std::cout << "cannot find .mnn file" << std::endl;
 
    return 0;
}