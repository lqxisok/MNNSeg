#pragma once

#include "segmnn.hpp"

#define WINDOWS

#ifdef WINDOWS
    #define DLLExport __declspec(dllexport)
#else
    #define DLLExport __attribute__(( visibility("default") ))
#endif

extern "C"
{
    DLLExport int fun(int a, int b)
    {
        return a+b;
    }

    DLLExport int initializeModel(const char* path, int numThread, int width, int height, int channel);

    DLLExport int processImage(uchar * imageData);

    DLLExport int runSession();

    DLLExport int getOutput(uchar * outputArray);

    DLLExport int releaseSession();

}




