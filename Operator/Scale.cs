﻿using UnityEngine;

namespace LibNoise.Operator
{
    /// <summary>
    /// Provides a noise module that scales the coordinates of the input value before
    /// returning the output value from a source module. [OPERATOR]
    /// </summary>
    public class Scale : SerializableModuleBase
    {
        #region Fields

        private double _x = 1.0;
        private double _y = 1.0;
        private double _z = 1.0;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of Scale.
        /// </summary>
        public Scale()
            : base(1)
        {
        }

        /// <summary>
        /// Initializes a new instance of Scale.
        /// </summary>
        /// <param name="input">The input module.</param>
        public Scale(SerializableModuleBase input)
            : base(1)
        {
            Modules[0] = input;
        }

        /// <summary>
        /// Initializes a new instance of Scale.
        /// </summary>
        /// <param name="x">The scaling on the x-axis.</param>
        /// <param name="y">The scaling on the y-axis.</param>
        /// <param name="z">The scaling on the z-axis.</param>
        /// <param name="input">The input module.</param>
        public Scale(double x, double y, double z, SerializableModuleBase input)
            : base(1)
        {
            UnityEngine.Debug.Log(input.GetType());
            Modules[0] = input;
            X = x;
            Y = y;
            Z = z;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the scaling factor on the x-axis.
        /// </summary>
        public double X
        {
            get { return _x; }
            set { _x = value; }
        }

        /// <summary>
        /// Gets or sets the scaling factor on the y-axis.
        /// </summary>
        public double Y
        {
            get { return _y; }
            set { _y = value; }
        }

        /// <summary>
        /// Gets or sets the scaling factor on the z-axis.
        /// </summary>
        public double Z
        {
            get { return _z; }
            set { _z = value; }
        }

        #endregion

        #region ModuleBase Members

        public override RenderTexture GetValueGPU(GPURenderingDatas renderingDatas)
        {
            Vector3 tmpScale = renderingDatas.scale;

            renderingDatas.scale = new Vector3(renderingDatas.scale.x + (float)_x, renderingDatas.scale.y + (float)_y, renderingDatas.scale.z + (float)_z);

            var input = Modules[0].GetValueGPU(renderingDatas);

            renderingDatas.scale = tmpScale;
            return input;

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
            System.Diagnostics.Debug.Assert(Modules[0] != null);
            return Modules[0].GetValueCPU(x * _x, y * _y, z * _z);
        }

        #endregion
    }
}