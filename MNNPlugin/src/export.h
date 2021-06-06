#pragma once

#include "segmnn.h"

extern "C" __declspec(dllexport)
unsigned char fun(uchar * image, int w, int h, int channel, int x, int y)
{
	return image[x * h * channel + y + 2];
}

extern "C" __declspec(dllexport)
int initializeModel(const char* path, int numThread, int width, int height, int channel);

extern "C" __declspec(dllexport)
int processImage(uchar * imageData);

extern "C" __declspec(dllexport)
int runSession();

extern "C" __declspec(dllexport)
int getOutput(uchar * outputArray);

extern "C" __declspec(dllexport)
int releaseSession();




