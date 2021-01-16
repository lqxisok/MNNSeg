#include "export.h"


int main(int argc, char **argv){

    initializeModel(4, 720, 1280);
    processImage();
    runSession();
    uchar * out =  (uchar *)getOutput();
    // I have test the output. It only contains 0 and 100 (the seg label is multiplied by 100 for visualization).
    // please change the file  `src/segmnn.cpp -> getOutput() -> result.at<uchar>(i, j) = index * 100;`
    releaseSession();

    std::cout << "done" << std::endl;
    return 0;
}