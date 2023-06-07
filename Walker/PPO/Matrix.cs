using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;

namespace Physics.Walker.PPO;

public class Matrix
{
    private readonly int _height; // rows
    private readonly int _length; // columns
    private readonly float[][] _values; // jagged arrays are more efficient than bi-dimensional arrays

    public static Matrix FromSize(int height, int length)
    {
        List<float[]> list = new List<float[]>();
        for (int i = 0; i < height; i++)
        {
            list.Add(new float[length]);
        }

        return new Matrix(list.ToArray());
    }

    public Matrix Clone()
    {
        Matrix newMatrix = Matrix.FromSize(_height, _length);
        for (int i = 0; i < _values.Length; i++)
        {
            for (int j = 0; j < _values[i].Length; j++)
            {
                newMatrix._values[i][j] = _values[i][j];
            }
        }

        return newMatrix;
    }

    public static Matrix FromValues(float[][] values)
    {
        return new Matrix(values);
    }
    
    public static Matrix FromValues(float[] values)
    {
        Matrix matrix = Matrix.FromSize(values.Length, 1);
        
        for (int i = 0; i < values.Length; i++)
        {
            matrix.SetValue(i, 0, values[i]);
        }

        return matrix;
    }

    public static Matrix FromRandom(int height, int length)
    {
        Matrix matrix = Matrix.FromSize(height, length);
        matrix.Randomise();
        return matrix;
    }

    public static Matrix FromZeroes(int height, int length)
    {
        Matrix matrix = Matrix.FromSize(height, length);
        matrix.Zero();
        return matrix;
    }
    
    private Matrix(float[][] values)
    {
        _values = values;
        _height = values.Length;
        _length = values[0].Length;
    }

    public int GetHeight()
    {
        return _height;
    }

    public int GetLength()
    {
        return _length;
    }

    public void Randomise()
    {
        Random random = new Random();
        
        for (int i = 0; i < _height; i++)
        {
            for (int j = 0; j < _length; j++)
            {
                float value = (float) (2f * random.NextDouble()) - 1f;
                _values[i][j] = value;
            }
        }
    }

    public void Zero()
    {
        for (int i = 0; i < _height; i++)
        {
            for (int j = 0; j < _length; j++)
            {
                _values[i][j] = 0;
            }
        }
    }
    
    public float Sum()
    {
        if (_length != 1) throw new Exception($"Invalid matrix dimensions: trying to get sum of a matrix which is not length 1.");
        
        float sum = 0;
        for (int i = 0; i < _height; i++)
        {
            sum += _values[i][0];
        }

        return sum;
    }
    
    public float Max()
    {
        if (_length != 1) throw new Exception($"Invalid matrix dimensions: trying to get max of a matrix which is not length 1.");
        
        float max = float.MinValue;
        for (int i = 0; i < _height; i++)
        {
            if (_values[i][0] > max) max = _values[i][0];
        }

        return max;
    }
    
    public float Mean()
    {
        float sum = Sum();
        return sum / _height;
    }

    public static Matrix Exponential(Matrix matrix)
    {
        return Matrix.PerformOperation(matrix, MathF.Exp);
    }
    
    // slower when using multiple threads
    public static Matrix operator * (Matrix matrixA, Matrix matrixB)
    {
        if (matrixA._length != matrixB._height) throw new Exception($"Invalid matrix dimensions: multiplying {matrixA._height}x{matrixA._length} matrix with {matrixB._height}x{matrixB._length}");
        Matrix newMatrix = Matrix.FromSize(matrixA._height, matrixB._length);
        
        for (int i = 0; i < newMatrix._height; i++)
        {
            for (int j = 0; j < newMatrix._length; j++)
            {
                newMatrix._values[i][j] = Multiply(i, j, matrixA, matrixB);
            }
        }
        
        return newMatrix;
    }
    
    public static Matrix operator * (Matrix matrix, float value)
    {
        Matrix newMatrix = Matrix.FromSize(matrix._height, matrix._length);
        
        for (int i = 0; i < matrix._height; i++)
        {
            for (int j = 0; j < matrix._length; j++)
            {
                newMatrix._values[i][j] = matrix._values[i][j] * value;
            }
        }
        
        return newMatrix;
    }
    
    public static Matrix operator / (Matrix matrix, float value)
    {
        Matrix newMatrix = Matrix.FromSize(matrix._height, matrix._length);
        
        for (int i = 0; i < matrix._height; i++)
        {
            for (int j = 0; j < matrix._length; j++)
            {
                newMatrix._values[i][j] = matrix._values[i][j] / value;
            }
        }
        
        return newMatrix;
    }

    public static Matrix SquareRoot(Matrix matrix)
    {
        Matrix newMatrix = Matrix.FromSize(matrix._height, matrix._length);
        
        for (int i = 0; i < matrix._height; i++)
        {
            for (int j = 0; j < matrix._length; j++)
            {
                newMatrix._values[i][j] = (float) Math.Sqrt(matrix._values[i][j]);
            }
        }
        
        return newMatrix;
    }
    
    // commutative
    public static Matrix operator * (float value, Matrix matrix)
    {
        Matrix newMatrix = Matrix.FromSize(matrix._height, matrix._length);
        
        for (int i = 0; i < matrix._height; i++)
        {
            for (int j = 0; j < matrix._length; j++)
            {
                newMatrix._values[i][j] = matrix._values[i][j] * value;
            }
        }
        
        return newMatrix;
    }
    
    public static Matrix operator + (Matrix matrixA, Matrix matrixB)
    {
        if (matrixA._length != matrixB._length || matrixA._height != matrixB._height) throw new Exception($"Invalid matrix dimensions, adding {matrixA._height}x{matrixA._length} matrix with {matrixB._height}x{matrixB._length}.");
        
        Matrix newMatrix = Matrix.FromSize(matrixA._height, matrixB._length);
        
        for (int i = 0; i < newMatrix._height; i++)
        {
            for (int j = 0; j < newMatrix._length; j++)
            {
                newMatrix._values[i][j] = matrixA._values[i][j] + matrixB._values[i][j];
            }
        }

        return newMatrix;
    }
    
    public static Matrix operator + (Matrix matrix, float value)
    {
        Matrix newMatrix = Matrix.FromSize(matrix._height, matrix._length);
        
        for (int i = 0; i < matrix._height; i++)
        {
            for (int j = 0; j < matrix._length; j++)
            {
                newMatrix._values[i][j] = matrix._values[i][j] + value;
            }
        }

        return newMatrix;
    }
    
    public static Matrix operator - (Matrix matrix, float value)
    {
        Matrix newMatrix = Matrix.FromSize(matrix._height, matrix._length);
        
        for (int i = 0; i < matrix._height; i++)
        {
            for (int j = 0; j < matrix._length; j++)
            {
                newMatrix._values[i][j] = matrix._values[i][j] - value;
            }
        }

        return newMatrix;
    }
    
    public static Matrix operator - (Matrix matrixA, Matrix matrixB)
    {
        if (matrixA._length != matrixB._length || matrixA._height != matrixB._height) throw new Exception($"Invalid matrix dimensions, subtracting {matrixA._height}x{matrixA._length} matrix with {matrixB._height}x{matrixB._length}.");
        
        Matrix newMatrix = Matrix.FromSize(matrixA._height, matrixB._length);
        
        for (int i = 0; i < newMatrix._height; i++)
        {
            for (int j = 0; j < newMatrix._length; j++)
            {
                newMatrix._values[i][j] = matrixA._values[i][j] - matrixB._values[i][j];
            }
        }

        return newMatrix;
    }
    
    public static Matrix operator - (Matrix matrix)
    {
        Matrix newMatrix = Matrix.FromSize(matrix._height, matrix._length);
        
        for (int i = 0; i < matrix._height; i++)
        {
            for (int j = 0; j < matrix._length; j++)
            {
                newMatrix._values[i][j] = -matrix._values[i][j];
            }
        }

        return newMatrix;
    }
    
    public static Matrix HadamardProduct(Matrix matrixA, Matrix matrixB)
    {
        if (matrixA._length != matrixB._length || matrixA._height != matrixB._height) throw new Exception($"Invalid matrix dimensions, multiplying {matrixA._height}x{matrixA._length} matrix with {matrixB._height}x{matrixB._length}.");
        Matrix newMatrix = Matrix.FromSize(matrixA._height, matrixB._length);
        
        for (int i = 0; i < newMatrix._height; i++)
        {
            for (int j = 0; j < newMatrix._length; j++)
            {
                newMatrix._values[i][j] = matrixA._values[i][j] * matrixB._values[i][j];
            }
        }
        
        return newMatrix;
    }
    
    public static Matrix HadamardDivision(Matrix matrixA, Matrix matrixB)
    {
        if (matrixA._length != matrixB._length || matrixA._height != matrixB._height) throw new Exception($"Invalid matrix dimensions, dividing {matrixA._height}x{matrixA._length} matrix with {matrixB._height}x{matrixB._length}.");
        Matrix newMatrix = Matrix.FromSize(matrixA._height, matrixB._length);
        
        for (int i = 0; i < newMatrix._height; i++)
        {
            for (int j = 0; j < newMatrix._length; j++)
            {
                newMatrix._values[i][j] = matrixA._values[i][j] / matrixB._values[i][j];
            }
        }
        
        return newMatrix;
    }

    public static Matrix Clip(Matrix matrix, float upper, float lower)
    {
        Matrix newMatrix = Matrix.FromSize(matrix._height, matrix._length);
        for (int i = 0; i < matrix._height; i++)
        {
            for (int j = 0; j < matrix._length; j++)
            {
                if (matrix._values[i][j] >= upper)
                {
                    newMatrix._values[i][j] = upper;
                } else if (matrix._values[i][j] <= lower)
                {
                    newMatrix._values[i][j] = lower;
                }
                else
                {
                    newMatrix._values[i][j] = matrix._values[i][j];
                }
            }
        }

        return newMatrix;
    }
    
    public static Matrix Clip(Matrix matrix, float upper)
    {
        Matrix newMatrix = Matrix.FromSize(matrix._height, matrix._length);
        for (int i = 0; i < matrix._height; i++)
        {
            for (int j = 0; j < matrix._length; j++)
            {
                if (matrix._values[i][j] > upper)
                {
                    newMatrix._values[i][j] = upper;
                } else
                {
                    newMatrix._values[i][j] = matrix._values[i][j];
                }
            }
        }

        return newMatrix;
    }

    public static Matrix Min(Matrix matrixA, Matrix matrixB)
    {
        if (matrixA._length != matrixB._length || matrixA._height != matrixB._height) throw new Exception($"Invalid matrix dimensions, comparing {matrixA._height}x{matrixA._length} matrix with {matrixB._height}x{matrixB._length}.");

        Matrix newMatrix = Matrix.FromSize(matrixA._height, matrixA._length);
        for (int i = 0; i < newMatrix._height; i++)
        {
            for (int j = 0; j < newMatrix._length; j++)
            {
                if (matrixA._values[i][j] < matrixB._values[i][j])
                {
                    newMatrix._values[i][j] = matrixA._values[i][j];
                }
                else
                {
                    newMatrix._values[i][j] = matrixB._values[i][j];
                }
            }
        }

        return newMatrix;
    }

    public static Matrix Transpose(Matrix matrix)
    {
        Matrix newMatrix = Matrix.FromSize(matrix._length, matrix._height);
        for (int i = 0; i < matrix._height; i++)
        {
            for (int j = 0; j < matrix._length; j++)
            {
                newMatrix._values[j][i] = matrix._values[i][j];
            }
        }

        return newMatrix;
    }
    
    public static Matrix Flatten(Matrix matrix)
    {
        Matrix newMatrix = Matrix.FromSize(matrix._height, 1);
        for (int i = 0; i < matrix._height; i++)
        {
            float sum = 0;
            
            for (int j = 0; j < matrix._length; j++)
            {
                sum += matrix._values[i][j];
            }

            newMatrix._values[i][0] = sum;
        }

        return newMatrix;
    }
    
    public static Matrix PerformOperation (Matrix matrix, Func<float, float> operation)
    {
        Matrix newMatrix = Matrix.FromSize(matrix._height, matrix._length);
        
        for (int i = 0; i < newMatrix._height; i++)
        {
            for (int j = 0; j < newMatrix._length; j++)
            {
                newMatrix._values[i][j] = operation(matrix._values[i][j]);
            }
        }

        return newMatrix;
    }

    private float[] GetRow(int rowNum)
    {
        return _values[rowNum];
    }
    
    private float[] GetColumn(int columnNum)
    {
        float[] column = new float[_height];
        for (int i = 0; i < _height; i++)
        {
            column[i] = _values[i][columnNum];
        }
        return column;
    }
    
    private static float Multiply(int rowNum, int columnNum, Matrix matrixA, Matrix matrixB)
    {
        float[] rowA = matrixA.GetRow(rowNum);
        float[] columnB = matrixB.GetColumn(columnNum); 
        
        return rowA.Select((row, i) => row * columnB[i]).Sum();
    }

    public float GetValue(int height, int length)
    {
        return _values[height][length];
    }

    public void SetValue(int height, int length, float value)
    {
        _values[height][length] = value;
    }

    public string GetSize()
    {
        return $"{_height}x{_length}";
    }
}