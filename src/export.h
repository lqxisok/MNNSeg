#ifndef EXPORT_MNN_H
#define EXPORT_MNN_H

#include "segmnn.hpp"


int initializeModel(int numThread, int height, int width);

int processImage();

int runSession();

void * getOutput();

int releaseSession();

#endif // EXPORTMNN_H



