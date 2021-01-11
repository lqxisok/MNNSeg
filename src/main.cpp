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