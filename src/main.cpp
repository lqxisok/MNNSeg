#include <iostream>
#include "Interpreter.hpp"
#include <string>
#include "MNNDefine.h"
#include "Tensor.hpp"
#include "ImageProcess.hpp"
#include <algorithm>
#include <iostream>
#include <string>
#include <vector>
#include <memory>
#include <chrono>
#include <expr/Expr.hpp>
#include <expr/ExprCreator.hpp>
#include <opencv2/opencv.hpp>
using cv::imread;
using std::string;
using cv::Mat;
using namespace cv;
using namespace MNN;
using namespace MNN::CV;

#include "segmnn.hpp"

int main(int argc, char **argv){
    if (argc <= 2) {
        fprintf(stderr, "Usage: %s <model-name>.mnn <image file>]\n", argv[0]);
        return 1;
    }

    segmnnsky::pcnetMNN testcase = segmnnsky::pcnetMNN();
    std::string modelName = argv[1];
    std::string imgName = argv[2];
    testcase.initInterpreter(modelName, 4, 720, 1280);
    testcase.processImage(imgName);
    testcase.runSession();
    cv::Mat result = testcase.getOutput();

    cv::imwrite("./mnnout.png", result);

    testcase.releaseSession();

    std::cout << "done" << std::endl;
    return 0;
}