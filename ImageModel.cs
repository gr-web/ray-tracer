﻿using RayTracer.Geometry;
using RayTracer.LinearAlgebra;
using RayTracer.Materials;
using RayTracer.Mathy;
using RayTracer.Textures;
using RayTracer.ViewPort;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RayTracer {
    class ImageModel {
        private int columns;
        private int rows;
        private Action updateImage;

        private Vec3 red = new Vec3(1,0,0);
        private Vec3 white = new Vec3(1,1,1);
        private Vec3 black = new Vec3(0,0,0);
        private Vec3 lightBlue = new Vec3(0.5f, 0.7f, 1.0f);

        private Camera camera;

        private int sampleCount = 10;
        private int maxBounceDepth = 10;

        private int ballPlacementDimension = 3;

        private float hFovDeg;

        private Hitable world;

        private Random rand = new Random(123);

        private float tMin = 0.001f;
        private float tMax = float.MaxValue;



        public ImageModel(int width, int height, Action updateImage, bool highRes) {
            this.updateImage = updateImage;
            this.rows = height;
            this.columns = width;
            if (highRes) {
                this.sampleCount = 1000;
                this.ballPlacementDimension = 11;
                this.maxBounceDepth = 50;
            }
            this.R = new int[this.columns, this.rows];
            this.G = new int[this.columns, this.rows];
            this.B = new int[this.columns, this.rows];
        }

        public int[,] R { get; }
        public int[,] G { get; }
        public int[,] B { get; }

        public void Update() {
            //Console.WriteLine("Good time to attach. Press anything to start raytracer.");
            //Console.ReadLine();
            CreateScene4();
            Console.WriteLine("Rendering started...");
            var sw = new Stopwatch();
            
            object threadLockObject = new object();
            Random rand = new Random();
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = 6;

            sw.Start();
            for (int y = 0; y < this.rows; y++) {
                if (y % (this.rows / 20) == 0) {
                    Console.WriteLine($"{100 * (float)y / this.rows} %");
                    Console.WriteLine($"Elapsed time: {sw.Elapsed.Minutes} min  {sw.Elapsed.Seconds} sec");
                }

                //for (int x = 0; x < this.columns; x++) {
                //    var color = RayTrace(x, y, rand);
                //    R[x, y] = (int)(255.99 * color.R);
                //    G[x, y] = (int)(255.99 * color.G);
                //    B[x, y] = (int)(255.99 * color.B);
                //}

                Parallel.For<Random>(0, this.columns, po,
                    () => { lock (threadLockObject) { return new Random(rand.Next()); } },
                    (x, loop, localRandom) => {
                        var color = RayTrace(x, y, localRandom);
                        R[x, y] = (int)(255.99 * (color.R));
                        G[x, y] = (int)(255.99 * (color.G));
                        B[x, y] = (int)(255.99 * (color.B));
                        return localRandom;
                    },
                    (x) => { });
            }
            sw.Stop();
            Console.WriteLine("Rendering completed!");
            Console.WriteLine($"Render time: {sw.Elapsed.Minutes} min  {sw.Elapsed.Seconds} sec");
            this.updateImage.Invoke();
        }

        /// <summary>
        /// Pdf 2, perlin noise
        /// </summary>
        private void CreateScene4() {
            // Set up camera
            this.hFovDeg = 35;
            var lookFrom = new Vec3(13, 2, 3);
            var lookAt = new Vec3(0, 0, 0);
            var lookUp = new Vec3(0, 1, 0);
            float focusDist = 10;
            float aperture = 0;
            this.camera = new CartesianCamera(this.rows, this.columns, this.hFovDeg, lookFrom, lookAt, lookUp,
                aperture, focusDist);

            // Add items
            var hitables = new List<Hitable>();
            var perlinTexture = new NoiseTexture();
            hitables.Add(new Sphere(new Vec3(0, -1000, 0), 1000, new Lambertian(perlinTexture)));
            hitables.Add(new Sphere(new Vec3(0, 2, 0), 2, new Lambertian(perlinTexture)));

            this.world = new HitableList(hitables);
        }

        /// <summary>
        /// Scene from pdf 2, texture; two checker balls touching.
        /// </summary>
        private void CreateScene3() {
            // Set up camera
            this.hFovDeg = 25;
            var lookFrom = new Vec3(13, 2, 3);
            var lookAt = new Vec3(0, 0, 0);
            var lookUp = new Vec3(0, 1, 0);
            float focusDist = 10;
            float aperture = 0;
            this.camera = new CartesianCamera(this.rows, this.columns, this.hFovDeg, lookFrom, lookAt, lookUp,
                aperture, focusDist);

            // Add items
            var hitables = new List<Hitable>();
            var oddTexture = new ConstantTexture(new Vec3(0.2f, 0.3f, 0.1f));
            var evenTexture = new ConstantTexture(new Vec3(0.9f, 0.9f, 0.9f));
            var checkerTexture = new CheckerTexture(evenTexture, oddTexture);

            hitables.Add(new Sphere(new Vec3(0, -10, 0), 10, new Lambertian(checkerTexture)));
            hitables.Add(new Sphere(new Vec3(0, 10, 0), 10, new Lambertian(checkerTexture)));

            this.world = new HitableList(hitables);
        }

        /// <summary>
        /// Scene with rectangle and three spheres.
        /// </summary>
        private void CreateScene2() {
            // Set up camera
            this.hFovDeg = 90;
            var lookFrom = new Vec3(0, 1, 1);
            var lookAt = new Vec3(0, 0.5f, 0);
            var lookUp = new Vec3(0, 1, 0);
            float focusDist = (lookAt - lookFrom).Length;
            float aperture = 0.00f;
            this.camera = new CartesianCamera(this.rows, this.columns, this.hFovDeg, lookFrom, lookAt, lookUp,
                aperture, focusDist);

            var hitables = new List<Hitable>();
            var recOrigin = new Vec3(2f, -2, -2);
            var u = new Vec3(0, 4 , 0);
            var v = new Vec3(-4, 0, 0);
            var material = new Metal(new Vec3(0.1f,0.1f,0.3f), 0.0f);
            hitables.Add(new Rectangle(recOrigin, u, v, material));

            var sphere = new Sphere(new Vec3(0,0,-1), 0.5f, new Metal(new Vec3(0.7f, 0.25f, 0), 0.1f));
            hitables.Add(sphere);
            sphere = new Sphere(new Vec3(1.3f, 0, -1), 0.5f, new Metal(new Vec3(0,0.25f,0.7f),0.025f));
            hitables.Add(sphere);
            sphere = new Sphere(new Vec3(-1.3f, 0, -1), 0.5f, new Metal(new Vec3(0.1f, 0.8f, 0.3f), 0.05f));
            hitables.Add(sphere);
            this.world = new HitableList(hitables);
        }

        /// <summary>
        /// Scene from pdf 1; three spheres with lots around.
        /// </summary>
        private void CreateScene1() {
            Console.WriteLine("Populating scene..");

            // Set up camera
            this.hFovDeg = 35;
            var lookFrom = new Vec3(13, 2, 3);
            var lookAt = new Vec3(0, 0, 0);
            var lookUp = new Vec3(0, 1, 0);
            float focusDist = (lookAt - lookFrom).Length;
            float aperture = 0.1f;
            this.camera = new CartesianCamera(this.rows, this.columns, this.hFovDeg, lookFrom, lookAt, lookUp,
                aperture, focusDist);

            // Add spheres
            var hitables = new List<Hitable>();
            var oddTexture = new ConstantTexture(new Vec3(0.2f, 0.3f, 0.1f));
            var evenTexture = new ConstantTexture(new Vec3(0.9f, 0.9f, 0.9f));
            var checkerTexture = new CheckerTexture(evenTexture, oddTexture);
            hitables.Add(new Sphere(new Vec3(0, -1000, 0), 1000, new Lambertian(checkerTexture)));
            Random random = new Random(10);
            int dim = this.ballPlacementDimension;
            for (int a = -dim; a < dim; a++) {
                for (int b = -dim; b < dim; b++) {
                    double chooseMat = random.NextDouble();
                    var center = new Vec3(a + 0.9f * (float)random.NextDouble(), 0.2f, b + 0.9f * (float)random.NextDouble());
                    if ((center - new Vec3(4, 0.2f, 0)).Length > 0.9) {
                        if (chooseMat < 0.4) { // diffuse
                            hitables.Add(new Sphere(center, 0.2f, new Lambertian(
                                new ConstantTexture(new Vec3((float)(random.NextDouble() * random.NextDouble()),
                                (float)(random.NextDouble() * random.NextDouble()),
                                (float)(random.NextDouble() * random.NextDouble()))))));
                        } else if (chooseMat < 0.65) { // metal
                            hitables.Add(new Sphere(center, 0.2f, new Metal(
                                new Vec3(0.5f * (float)(1 + random.NextDouble()),
                                0.5f * (float)(1 + random.NextDouble()),
                                0.5f * (float)(1 + random.NextDouble())),
                                0.5f * (float)random.NextDouble())));
                        } else { // glass
                            hitables.Add(new Sphere(center, 0.2f, new Dielectric(1.5f)));
                        }
                    }
                }
            }
            hitables.Add(new Sphere(new Vec3(0, 1, 0), 1, new Dielectric(1.5f)));
            hitables.Add(new Sphere(new Vec3(-4, 1, 0), 1, new Lambertian(new ConstantTexture(new Vec3(0.4f, 0.2f, 0.1f)))));
            hitables.Add(new Sphere(new Vec3(4, 1, 0), 1, new Metal(new Vec3(0.7f, 0.6f, 0.5f), 0)));
            //this.world = new HitableList(hitables);
            this.world = new BVHNode(hitables, this.tMin, this.tMax, new Random());
            Console.WriteLine("Populated Scene!");
        }

        private Vec3 RayTrace(int x, int y, Random random) {
            var pixelColor = new Vec3(0,0,0);
            for (int sample = 0; sample < this.sampleCount; sample++) {
                var ray = this.camera.GetRay(x,y, random);
                pixelColor += Color(ray, this.world, 0, random);
            }
            pixelColor /= (float) this.sampleCount;
            pixelColor.Clamp(0,1);
            //gamma = 2, col[i] ^ (1 / gamma)

            pixelColor.R = (float)Math.Sqrt(pixelColor.R);
            pixelColor.G = (float)Math.Sqrt(pixelColor.G);
            pixelColor.B = (float)Math.Sqrt(pixelColor.B);
            return pixelColor;
        }

        private Vec3 Color(Ray ray, Hitable world, int depth, Random random) {
            var record = new HitRecord();
            if (world.Hit(ray, this.tMin, this.tMax, ref record)) {
                Ray scatteredRay = null;
                Vec3 attenuation = null;
                if (depth < this.maxBounceDepth && record.Material.Scatter(ray, record, ref attenuation, ref scatteredRay, random)) {
                    return attenuation*Color(scatteredRay, this.world, depth + 1, random);
                } else {
                    return this.black;
                }
            } else {
                var unitDirection = ray.Direction.Normalized;
                float t = 0.5f * (unitDirection.Y + 1);
                return Vec3.Lerp(t, white, lightBlue);
            }
        }
    }
}
