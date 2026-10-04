using System.Runtime.InteropServices;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace ParkingVision.Vision;

/// <summary>
/// Runs a COCO-pretrained YOLO ONNX model and returns VEHICLE detections in ORIGINAL frame pixel coordinates.
/// Supports two output layouts (auto-detected from tensor shape):
///   [1, N, 6]       end-to-end (YOLOv10 / YOLO26 style: x1,y1,x2,y2,score,class) - NMS already done
///   [1, 4+nc, A]    classic YOLOv8/v11 (cx,cy,w,h + class scores) - NMS done here
/// InferenceSession.Run is thread-safe, so one detector instance can be shared by many camera pipelines.
/// </summary>
public sealed class YoloDetector : IDisposable
{
    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly VisionOptions _o;
    private readonly HashSet<int> _vehicles;

    public YoloDetector(VisionOptions options)
    {
        _o = options;
        _vehicles = options.VehicleClassIds.ToHashSet();
        _session = new InferenceSession(options.ModelPath);
        _inputName = _session.InputMetadata.Keys.First();
    }

    public IReadOnlyList<Detection> Detect(Mat frame)
    {
        int size = _o.InputSize;

        // ---- letterbox (keep aspect ratio, pad with grey 114) ----
        float scale = Math.Min(size / (float)frame.Width, size / (float)frame.Height);
        int nw = (int)Math.Round(frame.Width * scale), nh = (int)Math.Round(frame.Height * scale);
        int padX = (size - nw) / 2, padY = (size - nh) / 2;

        using var resized = new Mat();
        Cv2.Resize(frame, resized, new Size(nw, nh));
        using var canvas = new Mat(size, size, MatType.CV_8UC3, new Scalar(114, 114, 114));
        using (var roi = new Mat(canvas, new Rect(padX, padY, nw, nh))) resized.CopyTo(roi);
        Cv2.CvtColor(canvas, canvas, ColorConversionCodes.BGR2RGB);

        var buf = new byte[size * size * 3];
        Marshal.Copy(canvas.Data, buf, 0, buf.Length);

        var tensor = new DenseTensor<float>(new[] { 1, 3, size, size });
        var span = tensor.Buffer.Span;
        int plane = size * size;
        for (int i = 0, p = 0; i < plane; i++, p += 3)
        {
            span[i] = buf[p] / 255f;
            span[plane + i] = buf[p + 1] / 255f;
            span[2 * plane + i] = buf[p + 2] / 255f;
        }

        using var results = _session.Run(new[] { NamedOnnxValue.CreateFromTensor(_inputName, tensor) });
        var output = results.First().AsTensor<float>();
        var dims = output.Dimensions.ToArray();

        var raw = new List<Detection>();
        if (dims.Length == 3 && dims[2] == 6)
        {
            for (int i = 0; i < dims[1]; i++)
            {
                float score = output[0, i, 4];
                int cls = (int)output[0, i, 5];
                if (score < _o.MinScore || !_vehicles.Contains(cls)) continue;
                raw.Add(new Detection(output[0, i, 0], output[0, i, 1], output[0, i, 2], output[0, i, 3], score, cls));
            }
        }
        else if (dims.Length == 3 && dims[1] < dims[2])
        {
            int nc = dims[1] - 4, anchors = dims[2];
            for (int a = 0; a < anchors; a++)
            {
                float best = 0; int bestCls = -1;
                foreach (var c in _vehicles)
                {
                    if (c >= nc) continue;
                    float s = output[0, 4 + c, a];
                    if (s > best) { best = s; bestCls = c; }
                }
                if (best < _o.MinScore) continue;
                float cx = output[0, 0, a], cy = output[0, 1, a], w = output[0, 2, a], h = output[0, 3, a];
                raw.Add(new Detection(cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2, best, bestCls));
            }
            raw = Nms(raw, _o.NmsIou);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported YOLO output shape [{string.Join(",", dims)}]. Check the model in Netron.");
        }

        // ---- undo letterbox -> original pixels ----
        var list = new List<Detection>(raw.Count);
        foreach (var d in raw)
        {
            float x1 = Math.Clamp((d.X1 - padX) / scale, 0, frame.Width), x2 = Math.Clamp((d.X2 - padX) / scale, 0, frame.Width);
            float y1 = Math.Clamp((d.Y1 - padY) / scale, 0, frame.Height), y2 = Math.Clamp((d.Y2 - padY) / scale, 0, frame.Height);
            if (x2 - x1 < 2 || y2 - y1 < 2) continue;
            list.Add(d with { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2 });
        }
        return list;
    }

    private static List<Detection> Nms(List<Detection> dets, float iouThr)
    {
        var sorted = dets.OrderByDescending(d => d.Score).ToList();
        var keep = new List<Detection>();
        foreach (var d in sorted)
            if (keep.All(k => Iou(k, d) < iouThr)) keep.Add(d);
        return keep;
    }

    private static float Iou(Detection a, Detection b)
    {
        float ix = Math.Max(0, Math.Min(a.X2, b.X2) - Math.Max(a.X1, b.X1));
        float iy = Math.Max(0, Math.Min(a.Y2, b.Y2) - Math.Max(a.Y1, b.Y1));
        float inter = ix * iy;
        float union = (a.X2 - a.X1) * (a.Y2 - a.Y1) + (b.X2 - b.X1) * (b.Y2 - b.Y1) - inter;
        return union <= 0 ? 0 : inter / union;
    }

    public void Dispose() => _session.Dispose();
}
