using System.Diagnostics;
using LibNoise.Generator;
using UnityEngine;
using Xnoise;

namespace LibNoise.Operator
{
    /// <summary>
    /// Provides a noise module that that randomly displaces the input value before
    /// returning the output value from a source module. [OPERATOR]
    /// </summary>
    public class Turbulence : SerializableModuleBase
    {
        #region Constants

        private const double X0 = (12414.0 / 65536.0);
        private const double Y0 = (65124.0 / 65536.0);
        private const double Z0 = (31337.0 / 65536.0);
        private const double X1 = (26519.0 / 65536.0);
        private const double Y1 = (18128.0 / 65536.0);
        private const double Z1 = (60493.0 / 65536.0);
        private const double X2 = (53820.0 / 65536.0);
        private const double Y2 = (11213.0 / 65536.0);
        private const double Z2 = (44845.0 / 65536.0);

        #endregion

        #region Fields

        private double _power = 1.0;
        private readonly Perlin _xDistort;
        private readonly Perlin _yDistort;
        private readonly Perlin _zDistort;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of Turbulence.
        /// </summary>
        public Turbulence()
            : base(1)
        {
            _xDistort = new Perlin();
            _yDistort = new Perlin();
            _zDistort = new Perlin();
        }

        /// <summary>
        /// Initializes a new instance of Turbulence.
        /// </summary>
        /// <param name="input">The input module.</param>
        public Turbulence(SerializableModuleBase input)
            : base(1)
        {
            _xDistort = new Perlin();
            _yDistort = new Perlin();
            _zDistort = new Perlin();
            Modules[0] = input;
        }

        /// <summary>
        /// Initializes a new instance of Turbulence.
        /// </summary>
        public Turbulence(double power, SerializableModuleBase input)
            : this(new Perlin(), new Perlin(), new Perlin(), power, input)
        {
        }

        /// <summary>
        /// Initializes a new instance of Turbulence.
        /// </summary>
        /// <param name="x">The perlin noise to apply on the x-axis.</param>
        /// <param name="y">The perlin noise to apply on the y-axis.</param>
        /// <param name="z">The perlin noise to apply on the z-axis.</param>
        /// <param name="power">The power of the turbulence.</param>
        /// <param name="input">The input module.</param>
        public Turbulence(Perlin x, Perlin y, Perlin z, double power, SerializableModuleBase input)
            : base(1)
        {
            _xDistort = x;
            _yDistort = y;
            _zDistort = z;
            Modules[0] = input;
            Power = power;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the frequency of the turbulence.
        /// </summary>
        public double Frequency
        {
            get { return _xDistort.Frequency; }
            set
            {
                _xDistort.Frequency = value;
                _yDistort.Frequency = value;
                _zDistort.Frequency = value;
            }
        }

        /// <summary>
        /// Gets or sets the power of the turbulence.
        /// </summary>
        public double Power
        {
            get { return _power; }
            set { _power = value; }
        }

        /// <summary>
        /// Gets or sets the roughness of the turbulence.
        /// </summary>
        public int Roughness
        {
            get { return _xDistort.OctaveCount; }
            set
            {
                _xDistort.OctaveCount = value;
                _yDistort.OctaveCount = value;
                _zDistort.OctaveCount = value;
            }
        }

        /// <summary>
        /// Gets or sets the seed of the turbulence.
        /// </summary>
        public int Seed
        {
            get { return _xDistort.Seed; }
            set
            {
                _xDistort.Seed = value;
                _yDistort.Seed = value + 1;
                _zDistort.Seed = value + 2;
            }
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
            _materialGPU = XNoiseShaderCache.GetMaterial(XNoiseShaderPaths.Turbulence);

            var tmpTurbulence = renderingDatas.displacementMap;
            Vector3 tmpTranslate = renderingDatas.origin;
            //_materialGPU.SetTexture("_TextureA", Modules[0].GetValueGPU(renderingDatas));
            renderingDatas.origin = new Vector3((float)X0, (float)Y0, (float)Z0);
            _materialGPU.SetTexture("_PerlinA", new Perlin().GetValueGPU(renderingDatas));
            renderingDatas.origin = new Vector3((float)X1, (float)Y1, (float)Z1);
            _materialGPU.SetTexture("_PerlinB", new Perlin().GetValueGPU(renderingDatas));
            renderingDatas.origin = new Vector3((float)X2, (float)Y2, (float)Z2);
            _materialGPU.SetTexture("_PerlinC", new Perlin().GetValueGPU(renderingDatas));
            _materialGPU.SetFloat("_Power", (float)Power);
            renderingDatas.origin = tmpTranslate;
            renderingDatas.displacementMap = GetImage(_materialGPU, renderingDatas);

            var value = Modules[0].GetValueGPU(renderingDatas);
            //renderingDatas.displacementMap = tmpTurbulence;

            return value;
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
            var xd = x + (_xDistort.GetValueCPU(x + X0, y + Y0, z + Z0) * _power);
            var yd = y + (_yDistort.GetValueCPU(x + X1, y + Y1, z + Z1) * _power);
            var zd = z + (_zDistort.GetValueCPU(x + X2, y + Y2, z + Z2) * _power);
            return Modules[0].GetValueCPU(xd, yd, zd);
        }

        #endregion
    }
}