#include "export.h"

segmnnsky::pcnetMNN testcase = segmnnsky::pcnetMNN();

int initializeModel(int numThread, int height, int width){

    testcase.initInterpreter("G:/code/cpp/MNNSeg/pcnet.mnn", 4, 720, 1280);
    return 1;
}

int processImage(void * imageData){
    testcase.processImage(imageData);
    return 1;
}

int runSession(){
    testcase.runSession();
    return 1;
}

void * getOutput(){
    int out = testcase.getOutput();
    // return the output void * Ptr
    return testcase.getOutPtr();
}


int releaseSession(){
    testcase.releaseSession();
    return 1;
}