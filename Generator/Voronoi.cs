﻿using System;
using UnityEngine;
using XNoise;

namespace LibNoise.Generator
{
    /// <summary>
    /// Provides a noise module that outputs Voronoi cells. [GENERATOR]
    /// </summary>
    public class Voronoi : SerializableModuleBase
    {
        #region Fields

        private double _displacement = 1.0;
        private double _frequency = 1.0;
        private int _seed;
        private bool _distance;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of Voronoi.
        /// </summary>
        public Voronoi()
            : base(0)
        {
        }

        /// <summary>
        /// Initializes a new instance of Voronoi.
        /// </summary>
        /// <param name="frequency">The frequency of the first octave.</param>
        /// <param name="displacement">The displacement of the ridged-multifractal noise.</param>
        /// <param name="seed">The seed of the ridged-multifractal noise.</param>
        /// <param name="distance">Indicates whether the distance from the nearest seed point is applied to the output value.</param>
        public Voronoi(double frequency, double displacement, int seed, bool distance)
            : base(0)
        {
            Frequency = frequency;
            Displacement = displacement;
            Seed = seed;
            UseDistance = distance;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the displacement value of the Voronoi cells.
        /// </summary>
        public double Displacement
        {
            get { return _displacement; }
            set { _displacement = value; }
        }

        /// <summary>
        /// Gets or sets the frequency of the seed points.
        /// </summary>
        public double Frequency
        {
            get { return _frequency; }
            set { _frequency = value; }
        }

        /// <summary>
        /// Gets or sets the seed value used by the Voronoi cells.
        /// </summary>
        public int Seed
        {
            get { return _seed; }
            set { _seed = value; }
        }

        /// <summary>
        /// Gets or sets a value whether the distance from the nearest seed point is applied to the output value.
        /// </summary>
        public bool UseDistance
        {
            get { return _distance; }
            set { _distance = value; }
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
            _materialGPU = XNoiseShaderCache.GetMaterial(XNoiseShaderPaths.Voronoi);

            _materialGPU.SetFloat("_Displacement", (float)_displacement);
            _materialGPU.SetFloat("_Frequency", (float)_frequency);
            _materialGPU.SetInt("_Distance", _distance ? 2 : 0);
            _materialGPU.SetInt("_Seed", _seed);

            return GetImage(_materialGPU, renderingDatas, true);
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
            x *= _frequency;
            y *= _frequency;
            z *= _frequency;
            var xi = (x > 0.0 ? (int) x : (int) x - 1);
            var iy = (y > 0.0 ? (int) y : (int) y - 1);
            var iz = (z > 0.0 ? (int) z : (int) z - 1);
            var md = 2147483647.0;
            double xc = 0;
            double yc = 0;
            double zc = 0;
            for (var zcu = iz - 2; zcu <= iz + 2; zcu++)
            {
                for (var ycu = iy - 2; ycu <= iy + 2; ycu++)
                {
                    for (var xcu = xi - 2; xcu <= xi + 2; xcu++)
                    {
                        var xp = xcu + Utils.ValueNoise3D(xcu, ycu, zcu, _seed);
                        var yp = ycu + Utils.ValueNoise3D(xcu, ycu, zcu, _seed + 1);
                        var zp = zcu + Utils.ValueNoise3D(xcu, ycu, zcu, _seed + 2);
                        var xd = xp - x;
                        var yd = yp - y;
                        var zd = zp - z;
                        var d = xd * xd + yd * yd + zd * zd;
                        if (d < md)
                        {
                            md = d;
                            xc = xp;
                            yc = yp;
                            zc = zp;
                        }
                    }
                }
            }
            double v;
            if (_distance)
            {
                var xd = xc - x;
                var yd = yc - y;
                var zd = zc - z;
                v = (Math.Sqrt(xd * xd + yd * yd + zd * zd)) * Utils.Sqrt3 - 1.0;
            }
            else
            {
                v = 0.0;
            }
            return v + (_displacement * Utils.ValueNoise3D((int) (Math.Floor(xc)), (int) (Math.Floor(yc)),
                (int) (Math.Floor(zc)), 0));
        }

        #endregion
    }
}