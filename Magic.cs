using System;
using System.Collections.Generic;
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
        private const int MIN_TRACE_SPEED = 750; // Pixels/second (wand should keep moving, otherwise it's probably not a wand)
        private const int MAX_TRACE_SPEED = 3500;
        private const int MIN_TRACE_POINTS = 20;
        private const int MIN_TRACE_AREA = 7500;

        private const int TRACE_LINE_THICKNESS = 3;
        private const float VALID_TRACE_EFFECT_DURATION = 2f;

        private BackgroundSubtractorMOG mog;
        private SimpleBlobDetector blobby;
        private List<Point> tracePoints;
        private int dropoutFrames = 0;
        private int dropinFrames = 0;
        private DateTime lastTraceTime;
        private bool validTraceDetected = false;
        private DateTime traceDetectedEffectStart;
        
        internal Mat traceCanvas;
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
            tracePoints = new List<Point>();
            traceCanvas = new Mat(new Size(infraredFrameDescription.Width, infraredFrameDescription.Height), MatType.CV_32F);
        }
        internal unsafe int ProcessFrame(ushort* frameData, uint infraredFrameDataSize, FrameDescription infraredFrameDescription)
        {
            if (validTraceDetected)
            {
                traceCanvas.SetTo(new Scalar(0));
                var current_effect_time = (DateTime.Now - traceDetectedEffectStart).TotalSeconds;
                Cv2.Circle(traceCanvas,
                    new Point(infraredFrameDescription.Width / 2, infraredFrameDescription.Height / 2),
                    (int)(infraredFrameDescription.Width * (current_effect_time / VALID_TRACE_EFFECT_DURATION)),
                    Scalar.White,
                    thickness: 5);
                if (current_effect_time >= VALID_TRACE_EFFECT_DURATION)
                {
                    validTraceDetected = false;
                }
                return 0;
            }
            else
            {
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

        private void AddTracePoint(Point point)
        {
            tracePoints.Add(point);
            if (tracePoints.Count >= 2)
                Cv2.Line(traceCanvas,
                    tracePoints[tracePoints.Count - 2],
                    tracePoints[tracePoints.Count - 1],
                    Scalar.White,
                    thickness: TRACE_LINE_THICKNESS);
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
                        AddTracePoint(nearest_point);
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
                    ProcessTrace(tracePoints);
                }
            }
            tracePoints.Clear();
            traceCanvas.SetTo(new Scalar(0));
        }

        private void ProcessTrace(List<Point> points)
        {
            validTraceDetected = true;
            traceDetectedEffectStart = DateTime.Now;

        }
    }
}
