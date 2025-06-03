using System.Diagnostics;
using UnityEngine;
using Xnoise;

namespace LibNoise.Operator
{
    /// <summary>
    /// Provides a noise module that moves the coordinates of the input value before
    /// returning the output value from a source module. [OPERATOR]
    /// </summary>
    public class Translate : SerializableModuleBase
    {
        #region Fields

        private double _x = 1.0;
        private double _y = 1.0;
        private double _z = 1.0;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of Translate.
        /// </summary>
        public Translate()
            : base(1)
        {
        }

        /// <summary>
        /// Initializes a new instance of Translate.
        /// </summary>
        /// <param name="input">The input module.</param>
        public Translate(SerializableModuleBase input)
            : base(1)
        {
            Modules[0] = input;
        }

        /// <summary>
        /// Initializes a new instance of Translate.
        /// </summary>
        /// <param name="x">The translation on the x-axis.</param>
        /// <param name="y">The translation on the y-axis.</param>
        /// <param name="z">The translation on the z-axis.</param>
        /// <param name="input">The input module.</param>
        public Translate(double x, double y, double z, SerializableModuleBase input)
            : base(1)
        {
            Modules[0] = input;
            X = x;
            Y = y;
            Z = z;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the translation on the x-axis.
        /// </summary>
        public double X
        {
            get { return _x; }
            set { _x = value; }
        }

        /// <summary>
        /// Gets or sets the translation on the y-axis.
        /// </summary>
        public double Y
        {
            get { return _y; }
            set { _y = value; }
        }

        /// <summary>
        /// Gets or sets the translation on the z-axis.
        /// </summary>
        public double Z
        {
            get { return _z; }
            set { _z = value; }
        }

        #endregion

        #region ModuleBase Members

        /// <summary>
        /// Render this generator using a spherical shader.
        /// </summary>
        /// <param name="renderingDatas"></param>
        /// <returns>The generated image.</returns>
        /// 
        /// 
        public override RenderTexture GetValueGPU(GPURenderingDatas renderingDatas)
        {
            Vector3 tmpOrigin = renderingDatas.origin;

            renderingDatas.origin = new Vector3(renderingDatas.origin.x + (float)_x, renderingDatas.origin.y + (float)_y, renderingDatas.origin.z + (float)_z);

            var input = Modules[0].GetValueGPU(renderingDatas);

            renderingDatas.origin = tmpOrigin;
            return input;

            //_materialGPU.SetTexture("_TextureA", Modules[0].GetValueGPU(size, area, , projection));
            //_materialGPU.SetFloat("_X", (float)X);
            //_materialGPU.SetFloat("_Y", (float)Y);
            //_materialGPU.SetFloat("_Z", (float)Z);

            //return GetImage(_materialGPU, size);
        }
        /// <summary>
        /// Returns the output value for the given input coordinates.
        /// </summary>
        /// <param name="x">The input coordinate on the x-axis.</param>
        /// <param name="y">The input coordinate on the y-axis.</param>
        /// <param name="z">The input coordinate on the z-axis.</param>
        /// <returns>The resulting output value.</returns>
        public override double GetValueCPU(double x, double y, double z)
        {
            UnityEngine.Debug.Assert(Modules[0] != null);
            return Modules[0].GetValueCPU(x + _x, y + _y, z + _z);
        }

        #endregion
    }
}