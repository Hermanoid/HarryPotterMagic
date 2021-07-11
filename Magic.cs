using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using OpenCvSharp;

namespace Microsoft.Samples.Kinect.InfraredBasics
{
    public class Magic
    {
        private const int MAX_DROPOUT_FRAMES = 5;
        private const int MIN_DROPIN_FRAMES = 3;
        private const int MIN_TRACE_SPEED = 350; // Pixels/second (wand should keep moving, otherwise it's probably not a wand)
        private const int MAX_TRACE_SPEED = 3500;
        private const int MIN_TRACE_POINTS = 20;
        private const int MIN_TRACE_AREA = 7500;
        private const int TRACE_IMG_SIZE = 300;
        private const int TRACE_IMG_PADDING = 5;
        private const int TRACE_LINE_THICKNESS = 3;
        private const float EFFECT_TRACE_DURATION = 0.5f;
        private const float EFFECT_TRANSITION_DURATION = 1f;
        private const float EFFECT_ART_DURATION = 3.5f;
        private const string SAVE_PREFIX = "D:/Media/WandShots/";
        private const string ART_PREFIX = "C:/Source/HarryPotterMagic/Images/";
        const int FLICK_ANGLE_MIN = 70;
        const int FLICK_ANGLE_MAX = 110;
        const int FLICK_DISTANCE_MIN = 25;

        private readonly BackgroundSubtractorMOG mog;
        private readonly SimpleBlobDetector blobby;
        private readonly SpellAI spellAI;
        public readonly GameController gameController;
        private readonly List<Point> tracePoints;
        private int dropoutFrames = 0;
        private int dropinFrames = 0;
        private DateTime lastTraceTime;
        private bool validTraceDetected = false;
        private bool validTraceProcessed = false;
        private DateTime traceDetectedEffectStart;

        // traceCanvas is the preview actually seen by the user; traceFinal is a specially-sized mat for Spell Recognition.
        internal Mat traceCanvas;
        private Mat traceFinal = new Mat(new Size(TRACE_IMG_SIZE, TRACE_IMG_SIZE), MatType.CV_8UC1);

        // These two Mats are reserved for the completed-spell effect.
        private Mat spellArt;
        private Mat spellTrace;

        
        public Magic(FrameDescription infraredFrameDescription)
        {
            
            mog = BackgroundSubtractorMOG.Create();
            blobby = SimpleBlobDetector.Create(new SimpleBlobDetector.Params()
            {
                MinThreshold = 150,
                MaxThreshold = 255,
                FilterByColor = true,
                BlobColor = 255,
                FilterByArea = true,
                MinArea = 0.05f,
                MaxArea = 20,
                FilterByCircularity = true,
                MinCircularity = 0.5f,
                FilterByConvexity = true,
                MinConvexity = 0.5f,
                FilterByInertia = false
                //FilterByCircularity = true,
                //MinCircularity = 0.4f,
                //FilterByArea = true,
                //MaxArea = 10000
            });
            spellAI = new SpellAI();
            tracePoints = new List<Point>();
            traceCanvas = new Mat(new Size(infraredFrameDescription.Width, infraredFrameDescription.Height), MatType.CV_32F);
            gameController = new GameController();
        }
        public void Initialize()
        {
            gameController.Initialize();
        }
        internal unsafe int ProcessFrame(ushort* frameData, uint infraredFrameDataSize, FrameDescription infraredFrameDescription, bool captureSpell, string spellName)
        {
            // If Valid Trace has been detected, we either need to process it or complete the effect that follows it.
            if (validTraceDetected)
            {
                // Process the traceFinal produced during the last frame. 
                // The trace actually ended last frame, but instead of processing it right away,
                // we store that trace away until the next frame so the user can see their finished
                // trace before the CPU is plugged up with processing.
                if (!validTraceProcessed)
                {
                    if (captureSpell)
                    {
                        bool path_found = false;
                        string path = "";
                        int counter = 0;
                        while (!path_found)
                        {
                            path = Path.Combine(SAVE_PREFIX, $"{spellName}_{counter}.png");
                            if (!File.Exists(path))
                            {
                                path_found = true;
                            }
                            else
                            {
                                counter++;
                            }
                        }
                        Cv2.ImWrite(path, traceFinal);
                    }
                    else
                    {
                        // Starting the image as a larger image and then dilating/downsizing seems to produce better results than directly drawing the spell small.
                        Mat kernel = new Mat(5, 5, MatType.CV_8UC1);
                        kernel.SetTo(new Scalar(1));
                        Mat squeezed = new Mat();
                        Cv2.Dilate(traceFinal, squeezed, kernel, iterations: 2);
                        Cv2.Resize(squeezed, squeezed, new Size(SpellAI.TRACE_AI_SIZE, SpellAI.TRACE_AI_SIZE));
                        int pixels = SpellAI.TRACE_AI_SIZE * SpellAI.TRACE_AI_SIZE;
                        float[] sample = new float[pixels];
                        byte* data = (byte*)squeezed.Data;
                        for (int i = 0; i < pixels; i++)
                        {
                            sample[i] = (float)data[i];
                        }
                        var result = spellAI.Identify(sample);
                        gameController.TriggerSpell(result).Wait();
                        spellArt = new Mat();
                        Cv2.ImRead($"{ART_PREFIX}{result}.png", ImreadModes.Grayscale).ConvertTo(spellArt, MatType.CV_32FC1, 1/256.0);
                        //Cv2.PutText(traceCanvas, result.ToString(), new Point(5, traceCanvas.Height-5), HersheyFonts.HersheySimplex, 1.5, Scalar.White);
                    }
                    validTraceProcessed = true;
                }
                //traceCanvas.SetTo(new Scalar(0));
                var current_effect_time = (DateTime.Now - traceDetectedEffectStart).TotalSeconds;
                //Cv2.Circle(traceCanvas,
                //    new Point(infraredFrameDescription.Width / 2, infraredFrameDescription.Height / 2),
                //    (int)(infraredFrameDescription.Width * (current_effect_time / VALID_TRACE_EFFECT_DURATION)),
                //    Scalar.White,
                //    thickness: 5);
                if (current_effect_time <= EFFECT_TRACE_DURATION)
                {
                    // Do nothing. traceCanvas is set to the preview right as soon as it is created (in EndTrace), 
                    // so we don't need to update it here.
                }
                else if(current_effect_time <= EFFECT_TRACE_DURATION + EFFECT_TRANSITION_DURATION && !captureSpell)
                {
                    var ratio = (current_effect_time - EFFECT_TRACE_DURATION) / EFFECT_TRANSITION_DURATION;
                    Cv2.AddWeighted(spellTrace, 1-ratio, spellArt, ratio, 0, traceCanvas);
                }
                else if (current_effect_time <= EFFECT_TRACE_DURATION + EFFECT_TRANSITION_DURATION + EFFECT_ART_DURATION && !captureSpell)
                {
                    //Yes, this will be repeated a whole bunch of times for no reason, but I don't care enough to fix it. So.
                    spellArt.CopyTo(traceCanvas);
                }                
                else
                {
                    validTraceDetected = false;
                    validTraceProcessed = false;
                }
                
                return 0;
            }
            else
            {
                //If ValidTraceDetected is false, then we need to work on detecting a new one.
                var input = new Mat(infraredFrameDescription.Height, infraredFrameDescription.Width, MatType.CV_16U, (IntPtr)frameData);
                Mat converted = new Mat();
                input.ConvertTo(converted, MatType.CV_8U, 1.0 / 256.0);

                Mat mask = new Mat();
                mog.Apply(converted, mask);

                var keypoints = blobby.Detect(mask);
                if (!TraceDetected()) // Show the user's beautiful face while no spell is being drawn.
                {
                    //traceCanvas.SetTo(new Scalar(0));
                    //Cv2.BitwiseAnd(converted, mask, converted);
                    foreach (var keypoint in keypoints)
                    {
                        Cv2.Circle(converted, (Point)keypoint.Pt, 10 /*(int)keypoint.Size*/, Scalar.White, 2);
                    }
                    converted.ConvertTo(traceCanvas, MatType.CV_32F, 1.0 / 256.0);
                }

                // This function call produces the traceFinal image, which gets saved or processed by ML.
                // However, it does not do anything with that image; we intentionally wait a frame so that the user has a spell to look at before clogging up the CPU.
                ProcessKeypoints(keypoints); 
                converted.Dispose();
                mask.Dispose();
                input.Dispose();
                return keypoints.Count();
            }
        }

        private bool TraceDetected()
        {
            return tracePoints.Count > 0;
        }


        private void ProcessKeypoints(KeyPoint[] keypoints)
        {
            bool dropout = false;
            if (keypoints.Count() == 0)
            {
                dropout = true;
            }
            else
            {
                if (tracePoints.Count == 0)
                {
                    dropinFrames++;
                    if (dropinFrames >= MIN_DROPIN_FRAMES)
                    {
                        // Starting off with just the first detection should work well enough.
                        tracePoints.Add((Point)keypoints[0].Pt);
                        dropinFrames = 0;
                        traceCanvas.SetTo(new Scalar(0));
                    }
                }
                else
                {
                    var nearest_point = (Point)keypoints.OrderBy(keypoint => keypoint.Pt.DistanceTo(tracePoints.Last())).First().Pt;
                    var trace_speed = nearest_point.DistanceTo(tracePoints.Last()) / (DateTime.Now - lastTraceTime).TotalSeconds;
                    if (trace_speed < MIN_TRACE_SPEED || trace_speed > MAX_TRACE_SPEED)
                    {
                        dropout = true;
                    }
                    else
                    {
                        tracePoints.Add(nearest_point);
                        if (tracePoints.Count >= 2)
                            Cv2.Line(traceCanvas,
                                tracePoints[tracePoints.Count - 2],
                                tracePoints[tracePoints.Count - 1],
                                Scalar.White,
                                thickness: TRACE_LINE_THICKNESS);
                    }
                }
                dropoutFrames = 0;
            }

            if (dropout)
            {
                dropoutFrames++;
            }
            else
            {
                dropoutFrames = 0;
            }

            if (dropoutFrames >= MAX_DROPOUT_FRAMES && tracePoints.Count > 0)
            {
                EndTrace();
            }
            lastTraceTime = DateTime.Now;
        }
        private void EndTrace()
        {
            if (tracePoints.Count >= MIN_TRACE_POINTS)
            {
                FlickFilter(tracePoints);
                Point initial = tracePoints[0];
                Point top_left = new Point(initial.X, initial.Y);
                Point bottom_right = new Point(initial.X, initial.Y);
                foreach (var point in tracePoints)
                {
                    if (point.X < top_left.X)
                    {
                        top_left.X = point.X;
                    }
                    if (point.Y < top_left.Y)
                    {
                        top_left.Y = point.Y;
                    }
                    if (point.X > bottom_right.X)
                    {
                        bottom_right.X = point.X;
                    }
                    if (point.Y > bottom_right.Y)
                    {
                        bottom_right.Y = point.Y;
                    }
                }
                int area = (bottom_right.X - top_left.X) * (bottom_right.Y - top_left.Y);
                if (area > MIN_TRACE_AREA)
                {
                    validTraceDetected = true;
                    traceFinal.SetTo(new Scalar(0));
                    //int x = Math.Max(top_left.X - TRACE_IMG_PADDING, 0);
                    //int y = Math.Max(top_left.Y - TRACE_IMG_PADDING, 0);
                    int width = bottom_right.X - top_left.X;
                    int height = bottom_right.Y - top_left.Y;
                    //Rect from_roi = new Rect(new Point(x,y), new Size(width, height));

                    // Crop exactly the region with traces to fill entire final image, which is destined for Machine Learning classifier
                    //ResizeAndCenter(traceCanvas[from_roi], traceFinal);
                    double scale_factor = (double)TRACE_IMG_SIZE / (Math.Max(width, height) + TRACE_IMG_PADDING * 2);
                    int width_padding = 0;
                    int height_padding = 0;
                    if (width>height)
                    {
                        // If width is greater, set it to full width and apply padding to center height
                        int new_height = (int)(height * scale_factor);
                        height_padding = (TRACE_IMG_SIZE - new_height) / 2;
                    }
                    else
                    {
                        // If height is greater, set it to full height and apply padding to center width
                        int new_width = (int)(width * scale_factor);
                        width_padding = (TRACE_IMG_SIZE - new_width) / 2;
                    }

                    var mappedPoints = tracePoints.Select(p =>
                        new Point(
                            TRACE_IMG_PADDING + (p.X - top_left.X) * scale_factor + width_padding,
                            TRACE_IMG_PADDING + (p.Y - top_left.Y) * scale_factor + height_padding)).ToArray();
                    for (int i = 1; i < mappedPoints.Length; i++)
                    {
                        Cv2.Line(traceFinal, mappedPoints[i - 1], mappedPoints[i], Scalar.White, thickness: TRACE_LINE_THICKNESS);
                    }

                    // Once final trace image is filled, turn around and resize the final image to fill the canvas for viewing.
                    traceCanvas.SetTo(new Scalar(0));
                    spellTrace = new Mat(traceCanvas.Size(), traceCanvas.Type());
                    ResizeAndCenter(traceFinal, spellTrace, MatType.CV_32F);
                    spellTrace.CopyTo(traceCanvas);

                    //ProcessTrace(tracePoints);
                    traceDetectedEffectStart = DateTime.Now;
                }
            }
            tracePoints.Clear();
        }


        /// <summary>
        /// Filters out the fancy "flick" you do to end a spell
        /// </summary>
        private void FlickFilter(List<Point> tracePoints)
        {
            bool still_filtering = true;
            while (still_filtering)
            {
                Point last_point = tracePoints[tracePoints.Count - 1];
                Point next_point = tracePoints[tracePoints.Count - 2];
                Point vector = new Point(last_point.X - next_point.X, -(last_point.Y - next_point.Y)); //Negative y because images store y values opposite normal math
                double angle = Math.Atan2(vector.Y, vector.X) * 180 / Math.PI;
                double distance = Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
                if (angle > FLICK_ANGLE_MIN && angle < FLICK_ANGLE_MAX && distance > FLICK_DISTANCE_MIN)
                {
                    tracePoints.RemoveAt(tracePoints.Count - 1);
                }
                else
                {
                    still_filtering = false;
                }
            }
        }
        private void ResizeAndCenter(Mat source, Mat dest, MatType mat_type)
        {
            Rect to_roi;
            double width_ratio = (double)source.Width / dest.Width;
            double height_ratio = (double)source.Height / dest.Height;
            bool is_wide = width_ratio > height_ratio; // Is this image wider than tall relative to its destination
            double scale_factor = 1 / (is_wide ? width_ratio : height_ratio);
            int height_padding = 0;
            int width_padding = 0;
            if (is_wide)
            {
                // If width is greater, set it to full width and apply padding to center height
                int new_height = (int)(source.Height * scale_factor);
                height_padding = (dest.Height - new_height) / 2;
                to_roi = new Rect(0, height_padding, dest.Width, new_height);
            }
            else
            {
                // If height is greater, set it to full height and apply padding to center width
                int new_width = (int)(source.Width * scale_factor);
                width_padding = (dest.Width - new_width) / 2;
                to_roi = new Rect(width_padding, 0, new_width, dest.Height);
            }
            Mat resized = new Mat(); // For whatever reason OpenCV likes a mat in the middle here.
            Cv2.Resize(source, resized, to_roi.Size);
            resized.CopyMakeBorder(height_padding, height_padding, width_padding, width_padding, BorderTypes.Constant).ConvertTo(dest, mat_type);
            resized.Dispose();
        }
    }
}
