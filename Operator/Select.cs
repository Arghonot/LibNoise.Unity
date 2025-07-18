﻿using System.Diagnostics;
using UnityEngine;
using XNoise;

namespace LibNoise.Operator
{
    /// <summary>
    /// Provides a noise module that outputs the value selected from one of two source
    /// modules chosen by the output value from a control module. [OPERATOR]
    /// </summary>
    public class Select : SerializableModuleBase
    {
        #region Fields

        private double _fallOff;
        private double _raw;
        private double _min = -1.0;
        private double _max = 1.0;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of Select.
        /// </summary>
        public Select()
            : base(3)
        {
        }

        /// <summary>
        /// Initializes a new instance of Select.
        /// </summary>
        /// <param name="inputA">The first input module.</param>
        /// <param name="inputB">The second input module.</param>
        /// <param name="controller">The controller module.</param>
        public Select(SerializableModuleBase inputA, SerializableModuleBase inputB, SerializableModuleBase controller)
            : base(3)
        {
            Modules[0] = inputA;
            Modules[1] = inputB;
            Modules[2] = controller;
        }

        /// <summary>
        /// Initializes a new instance of Select.
        /// </summary>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <param name="fallOff">The falloff value at the edge transition.</param>
        /// <param name="inputA">The first input module.</param>
        /// <param name="inputB">The second input module.</param>
        public Select(double min, double max, double fallOff, SerializableModuleBase inputA, SerializableModuleBase inputB)
            : this(inputA, inputB, null)
        {
            _min = min;
            _max = max;
            FallOff = fallOff;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the controlling module.
        /// </summary>
        public SerializableModuleBase Controller
        {
            get { return Modules[2]; }
            set
            {
                System.Diagnostics.Debug.Assert(value != null);
                Modules[2] = value;
            }
        }

        /// <summary>
        /// Gets or sets the falloff value at the edge transition.
        /// </summary>
		/// <remarks>
		/// Called SetEdgeFallOff() on the original LibNoise.
		/// </remarks>
        public double FallOff
        {
            get { return _fallOff; }
            set
            {
                var bs = _max - _min;
                _raw = value;
                _fallOff = (value > bs / 2) ? bs / 2 : value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum, and re-calculated the fall-off accordingly.
        /// </summary>
        public double Maximum
        {
            get { return _max; }
            set
            {
                _max = value;
                FallOff = _raw;
            }
        }

        /// <summary>
		/// Gets or sets the minimum, and re-calculated the fall-off accordingly.
        /// </summary>
        public double Minimum
        {
            get { return _min; }
            set
            {
                _min = value;
                FallOff = _raw;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets the bounds.
        /// </summary>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        public void SetBounds(double min, double max)
        {
            System.Diagnostics.Debug.Assert(min < max);
            _min = min;
            _max = max;
            FallOff = _fallOff;
        }

        #endregion

        #region ModuleBase Members

        /// <summary>
        /// Render this generator using a spherical shader.
        /// </summary>
        /// <param name="renderingDatas"></param>
        /// <returns>The generated image.</returns>
        public override RenderTexture GetValueGPU(GPURenderingDatas renderingDatas)
        {
            _materialGPU = XNoiseShaderCache.GetMaterial(XNoiseShaderPaths.Select);

            _materialGPU.SetTexture("_TextureA", Modules[0].GetValueGPU(renderingDatas));
            _materialGPU.SetTexture("_TextureB", Modules[1].GetValueGPU(renderingDatas));
            _materialGPU.SetTexture("_TextureC", Modules[2].GetValueGPU(renderingDatas));
            _materialGPU.SetFloat("_FallOff", (float)FallOff);

            return GetImage(_materialGPU, renderingDatas);
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
            System.Diagnostics.Debug.Assert(Modules[1] != null);
            System.Diagnostics.Debug.Assert(Modules[2] != null);
            var cv = Modules[2].GetValueCPU(x, y, z);
            if (_fallOff > 0.0)
            {
                double a;
                if (cv < (_min - _fallOff))
                {
                    return Modules[0].GetValueCPU(x, y, z);
                }
                if (cv < (_min + _fallOff))
                {
                    var lc = (_min - _fallOff);
                    var uc = (_min + _fallOff);
                    a = Utils.MapCubicSCurve((cv - lc) / (uc - lc));
                    return Utils.InterpolateLinear(Modules[0].GetValueCPU(x, y, z),
                        Modules[1].GetValueCPU(x, y, z), a);
                }
                if (cv < (_max - _fallOff))
                {
                    return Modules[1].GetValueCPU(x, y, z);
                }
                if (cv < (_max + _fallOff))
                {
                    var lc = (_max - _fallOff);
                    var uc = (_max + _fallOff);
                    a = Utils.MapCubicSCurve((cv - lc) / (uc - lc));
                    return Utils.InterpolateLinear(Modules[1].GetValueCPU(x, y, z),
                        Modules[0].GetValueCPU(x, y, z), a);
                }
                return Modules[0].GetValueCPU(x, y, z);
            }
            if (cv < _min || cv > _max)
            {
                return Modules[0].GetValueCPU(x, y, z);
            }
            return Modules[1].GetValueCPU(x, y, z);
        }

        #endregion
    }
}