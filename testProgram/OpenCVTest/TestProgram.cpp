// TestProgram.cpp : �������̨Ӧ�ó������ڵ㡣
//

#include "stdafx.h"

using namespace cv;
int _tmain(int argc, _TCHAR* argv[])
{
	Mat img = imread("me.jpg");
	if (img.empty())
	{
		return -1;
	}
	imshow("me", img);
	waitKey();
	return 0;
}

