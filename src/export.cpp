#include "export.h"


segmnnsky::pcnetMNN testcase = segmnnsky::pcnetMNN();

int initializeModel(const char * path, int numThread, int width, int height, int channel){

    return testcase.initInterpreter(path, numThread, width, height, channel);
}

int processImage(uchar * imageData){
    return testcase.processImage(imageData);
}

int runSession(){
    return testcase.runSession();
}

int getOutput(uchar * outputArray){
    return testcase.getOutput(outputArray);
    // return the output void * Ptr
    //return testcase.getOutPtr();
}

int releaseSession(){
    testcase.releaseSession();
    return 1;
}