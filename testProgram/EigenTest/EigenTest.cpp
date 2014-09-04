// EigenTest.cpp : 定义控制台应用程序的入口点。
//

#include "stdafx.h"
#include <Eigen>
#include <Dense>

int _tmain(int argc, _TCHAR* argv[])
{
	Eigen::MatrixXd mat;
	int nRows = 25;
	int nCols = 25;
	mat.setOnes(nRows, nCols);
	for (int i = 0; i < nRows; i++)
	{
		for (int j = 0; j < nCols; ++j)
		{
			mat(i,j) = rand();
		}
	}
	return 0;
}
