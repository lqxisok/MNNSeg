#include "export.h"
#include <opencv2/opencv.hpp>

segmnnsky::pcnetMNN testcase = segmnnsky::pcnetMNN();
cv::Mat result;


int initializeModel(int numThread, int height, int width){

    testcase.initInterpreter("/root/github/MNNSeg/pcnet2.mnn", 4, 720, 1280);
    return 1;
}

int processImage(){
    testcase.processImage("/root/github/MNNSeg/test.jpg");
    return 1;
}

int runSession(){
    testcase.runSession();
    return 1;
}

void * getOutput(){
    result = testcase.getOutput();
    cv::imwrite("./mnnout.png", result);
    void * returnRes = result.data;
    return returnRes;
}


int releaseSession(){
    testcase.releaseSession();
    return 1;
}