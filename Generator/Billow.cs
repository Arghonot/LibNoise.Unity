using System;
using UnityEngine;

namespace LibNoise.Generator
{
    /// <summary>
    /// Provides a noise module that outputs a three-dimensional billowy noise. [GENERATOR]
    /// </summary>
    public class Billow : SerializableModuleBase
    {
        #region Fields

        private Material _materialGPU;
        private double _frequency = 1.0;
        private double _lacunarity = 2.0;
        private QualityMode _quality = QualityMode.Medium;
        private int _octaveCount = 6;
        private double _persistence = 0.5;
        private int _seed;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of Billow.
        /// </summary>
        public Billow()
            : base(0)
        {
        }

        /// <summary>
        /// Initializes a new instance of Billow.
        /// </summary>
        /// <param name="frequency">The frequency of the first octave.</param>
        /// <param name="lacunarity">The lacunarity of the billowy noise.</param>
        /// <param name="persistence">The persistence of the billowy noise.</param>
        /// <param name="octaves">The number of octaves of the billowy noise.</param>
        /// <param name="seed">The seed of the billowy noise.</param>
        /// <param name="quality">The quality of the billowy noise.</param>
        public Billow(double frequency, double lacunarity, double persistence, int octaves, int seed,
            QualityMode quality)
            : base(0)
        {
            Frequency = frequency;
            Lacunarity = lacunarity;
            OctaveCount = octaves;
            Persistence = persistence;
            Seed = seed;
            Quality = quality;
        }

        #endregion

        #region Properties
        protected override string GetPlanarShaderName() => "Xnoise/Generators/BillowPlanar";
        protected override string GetSphericalShaderName() => "Xnoise/Generators/BillowSpherical";
        protected override string GetCylindricalShaderName() => throw new NotImplementedException();

        /// <summary>
        /// Gets or sets the frequency of the first octave.
        /// <para>Frequency represents the number of cycles per unit length that a generation module outputs.</para>
        /// </summary>
        public double Frequency
        {
            get { return _frequency; }
            set { _frequency = value; }
        }

        /// <summary>
        /// Gets or sets the lacunarity of the billowy noise.
        /// <para>A multiplier that determines how quickly the frequency increases for each successive octave</para>
        /// </summary>
        public double Lacunarity
        {
            get { return _lacunarity; }
            set { _lacunarity = value; }
        }

        /// <summary>
        /// Gets or sets the quality of the billowy noise.
        /// </summary>
        public QualityMode Quality
        {
            get { return _quality; }
            set { _quality = value; }
        }

        /// <summary>
        /// Gets or sets the number of octaves of the billowy noise.
        /// <para>Adding more octaves increases the detail of the perlin noise, but with the drawback of increasing the calculation time.</para>
        /// </summary>
        public int OctaveCount
        {
            get { return _octaveCount; }
            set { _octaveCount = Mathf.Clamp(value, 1, Utils.OctavesMaximum); }
        }

        /// <summary>
        /// Gets or sets the persistence of the billowy noise.
        /// <para>A multiplier that determines how quickly the amplitudes diminish for each successive octave.</para>
        /// </summary>
        public double Persistence
        {
            get { return _persistence; }
            set { _persistence = value; }
        }

        /// <summary>
        /// Gets or sets the seed of the billowy noise.
        /// </summary>
        public int Seed
        {
            get { return _seed; }
            set { _seed = value; }
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
            _materialGPU = new Material(Shader.Find(GetCorrespondingShader(renderingDatas.projection)));

            _materialGPU.SetFloat("_Frequency", (float)_frequency);
            _materialGPU.SetFloat("_Lacunarity", (float)_lacunarity);
            _materialGPU.SetFloat("_Persistence", (float)_persistence);
            _materialGPU.SetFloat("_Octaves", _octaveCount);
            _materialGPU.SetVector("_OffsetPosition", renderingDatas.origin);
            _materialGPU.SetVector("_Rotation", renderingDatas.quaternionRotation);
            _materialGPU.SetFloat("_Seed", (float)_seed);
            
            return GetImage(_materialGPU, renderingDatas.size);
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
            var value       = 0.0;
            var amplitude   = 1.0;

            x *= _frequency;
            y *= _frequency;
            z *= _frequency;
            for (var i = 0; i < _octaveCount; i++)
            {
                var nx = Utils.MakeInt32Range(x);
                var ny = Utils.MakeInt32Range(y);
                var nz = Utils.MakeInt32Range(z);
                var seed = (_seed + i) & 0xffffffff;
                var signal = Utils.GradientCoherentNoise3D(nx, ny, nz, seed, _quality);
                signal = 2.0 * Math.Abs(signal) - 1.0;
                value += signal * amplitude;
                x *= _lacunarity;
                y *= _lacunarity;
                z *= _lacunarity;
                amplitude *= _persistence;
            }
            return value + 0.5;
        }

        #endregion
    }
}