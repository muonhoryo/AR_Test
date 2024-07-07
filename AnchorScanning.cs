
using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public sealed class AnchorScanning : MonoBehaviour
{
    public event Action<Vector3> FoundAnchorEvent = delegate { };
    public event Action<string> ChangeAnchorScanningStateEvent = delegate { };

    /// <summary>
    /// Half width with rounding to lower integer
    /// </summary>
    [SerializeField] private int ColorThreshold;
    [SerializeField][Range(0, 1000)] private int SquareCheckingAccuracy;
    [SerializeField] private float SquareFitDistance;

    [SerializeField] private GameObject CameraOffsetObject;
    [SerializeField] private BGRendering TextureSource;
    [SerializeField] private Text DebugText;

    private Thread AnchorCheckingThread;
    private AnchorChecker Checker;

    public string CurrentCheckingState_ => Checker.State_.ToString();

    private sealed class AnchorChecker
    {
        public event Action<string> ErrorEvent = delegate { };
        public event Action<AnchorChecker_State> AnchorCheckingStateChangedEvent = delegate { };

        public enum AnchorChecker_State
        {
            None,
            Inactive,
            FoundCameraWorldPosition,
            Initialization,
            BGFinding_BottomSlicing,
            BGFinding_RightSlicing,
            BGFinding_TopSlicing,
            BGFinding_LeftSlicing,
            BGCashing,
            VertCheckingInitialization,
            SQRBordersFinding_Bottom,
            SQRBordersFinding_Right,
            SQRBordersFinding_Top,
            SQRBordersFinding_Left,
            SquareCashing,
            SQRDeformationChecking
        }
        private AnchorChecker() { }
        public AnchorChecker(int ColorThreshold, int SquareCheckingAccuracy,
            float SquareFitDistance)
        {
            this.ColorThreshold = ColorThreshold;
            this.SquareCheckingAccuracy = SquareCheckingAccuracy;
            this.SquareFitDistance = SquareFitDistance;
        }

        private int ColorThreshold;
        private int SquareCheckingAccuracy;
        private float SquareFitDistance;

        public Vector3 CameraWorldPosition_ { get; private set; }
        private AnchorChecker_State State = AnchorChecker_State.None;
        public AnchorChecker_State State_
        {
            get => State;
            private set
            {
                State = value;
                AnchorCheckingStateChangedEvent.Invoke(State);
            }
        }

        public void CheckAnchor(Color32[] image, int width, int height)
        {
            try
            {
                if (State_ != AnchorChecker_State.Inactive &&
                    State_ != AnchorChecker_State.FoundCameraWorldPosition &&
                    State_ != AnchorChecker_State.None)
                    return;
                {
                    bool IsBlackPixel(Color32 pixel)
                    {
                        return pixel.r < ColorThreshold &&
                            pixel.g < ColorThreshold &&
                            pixel.b < ColorThreshold;
                    }
                    bool IsWhitePixel(Color32 pixel)
                    {
                        return pixel.r > ColorThreshold &&
                            pixel.g > ColorThreshold && pixel.b > ColorThreshold;
                    }
                    void ExitCheckingAction()
                    {
                        State_ = AnchorChecker_State.Inactive;
                    }
                    State_ = AnchorChecker_State.Initialization;

                    //BG Finding

                    BG_Bottom = BG_Right = BG_Left = BG_Top = -1;

                    State_ = AnchorChecker_State.BGFinding_TopSlicing;
                    {
                        void FindBottomOfBG()
                        {
                            for (i = 0; i < image.Length; i++) //Find bottom of bg
                            {
                                if (IsWhitePixel(image[i]))
                                {
                                    BG_Bottom = i / width;
                                    return;
                                }
                            }
                        }
                        FindBottomOfBG();
                    }
                    if (BG_Bottom <= -1) //Whole image is checked, but white pixel didn't found
                    {
                        ExitCheckingAction();
                        return;
                    }
                    State_ = AnchorChecker_State.BGFinding_RightSlicing;
                    BG_HorSidesSlicingStep = width;
                    {
                        void FindRightOfBG()
                        {
                            maxI = width * height;
                            for (j = width - 1; j >= 0; j--)
                            {
                                for (i = j + BG_Bottom * width; i < maxI; i += BG_HorSidesSlicingStep)
                                {
                                    if (IsWhitePixel(image[i]))
                                    {
                                        BG_Right = j;
                                        return;
                                    }
                                }
                            }
                        }
                        FindRightOfBG();
                    }
                    State_ = AnchorChecker_State.BGFinding_TopSlicing;
                    {
                        void FindTopOfBG()
                        {
                            j = height - 1;
                            for (; j >= BG_Bottom; j--)
                            {
                                for (i = j * width, maxI = i + BG_Right; i <= maxI; i++)
                                {
                                    if (IsWhitePixel(image[i]))
                                    {
                                        BG_Top = j;
                                        return;
                                    }
                                }
                            }
                        }
                        FindTopOfBG();
                    }
                    State_ = AnchorChecker_State.BGFinding_LeftSlicing;
                    {
                        void FindLeftOfBG()
                        {
                            j = 0;
                            for (maxI = (BG_Right + 1) * (BG_Top + 1); j <= BG_Right; j++)
                            {
                                for (i = BG_Bottom; i < maxI && i % width <= BG_Right; i += BG_HorSidesSlicingStep)
                                {
                                    if (IsWhitePixel(image[i]))
                                    {
                                        BG_Left = j;
                                        return;
                                    }
                                }
                            }
                        }
                        FindLeftOfBG();
                    }

                    //BG Converting

                    State_ = AnchorChecker_State.BGCashing;

                    BGColumnsCount = BG_Right - BG_Left - SquareCheckingAccuracy * 2 + 1;
                    BGRowsCount = BG_Top - BG_Bottom - SquareCheckingAccuracy * 2 + 1;

                    if (BGColumnsCount < SquareCheckingAccuracy ||
                        BGRowsCount < SquareCheckingAccuracy)
                    {
                        ExitCheckingAction();
                        return;
                    }


                    imageMatrix =
                        new Color32
                        [BGRowsCount] //row count
                        [];
                    {
                        maxI = BGRowsCount;
                        maxJ = BGColumnsCount;
                        for (i = 0; i < maxI; i++)
                        {
                            Color32[] arr = new Color32[BGColumnsCount];
                            for (j = 0; j < maxJ; j++)
                            {
                                arr[j] = image[j + SquareCheckingAccuracy + BG_Left + (i + SquareCheckingAccuracy + BG_Bottom) * width];
                            }
                            imageMatrix[i] = arr;
                        }
                    }

                    //Checking vertexes

                    State_ = AnchorChecker_State.VertCheckingInitialization;

                    //Finding borders of square

                    State_ = AnchorChecker_State.SQRBordersFinding_Bottom;
                    {
                        void FindBottomOfSQR()
                        {
                            for (j = 0; j < BGRowsCount; j++)
                            {
                                for (i = 0; i < BGColumnsCount; i++)
                                {
                                    if (IsBlackPixel(imageMatrix[j][i]))
                                    {
                                        sqr_bottom = j;
                                        return;
                                    }
                                }
                            }
                        }
                        FindBottomOfSQR();
                    }
                    if (sqr_bottom <= -1) //Whole bg is checked, but black pixel didn't found
                    {
                        ExitCheckingAction();
                        return;
                    }
                    State_ = AnchorChecker_State.SQRBordersFinding_Right;
                    {
                        void FindRightOfSQR()
                        {
                            for (i = BGColumnsCount - 1; i >= 0; i--)
                            {
                                for (j = sqr_bottom; j < BGRowsCount; j++)
                                {
                                    if (IsBlackPixel(imageMatrix[j][i]))
                                    {
                                        sqr_right = i;
                                        return;
                                    }
                                }
                            }
                        }
                        FindRightOfSQR();
                    }
                    if (sqr_right <= -1)
                    {
                        ExitCheckingAction();
                        return;
                    }
                    State_ = AnchorChecker_State.SQRBordersFinding_Top;
                    {
                        void FindTopOfSQR()
                        {
                            for (j = BGRowsCount - 1; j >= sqr_bottom; j--)
                            {
                                for (i = 0; i < BGColumnsCount; i++)
                                {
                                    if (IsBlackPixel(imageMatrix[j][i]))
                                    {
                                        sqr_top = j;
                                        return;
                                    }
                                }
                            }
                        }
                        FindTopOfSQR();
                    }
                    if (sqr_top <= -1)
                    {
                        ExitCheckingAction();
                        return;
                    }
                    State_ = AnchorChecker_State.SQRBordersFinding_Left;
                    {
                        void FindLeftOfSQR()
                        {
                            for (i = 0; i <= sqr_right; i++)
                            {
                                for (j = sqr_bottom; j <= sqr_top; j++)
                                {
                                    if (IsBlackPixel(imageMatrix[j][i]))
                                    {
                                        sqr_left = i;
                                        return;
                                    }
                                }
                            }
                        }
                        FindLeftOfSQR();
                    }
                    if (sqr_left <= -1)
                    {
                        ExitCheckingAction();
                        return;
                    }

                    //Square cashing

                    State_ = AnchorChecker_State.SquareCashing;

                    sqr_width = sqr_right - sqr_left + 1;
                    sqr_height = sqr_top - sqr_bottom + 1;

                    if(sqr_width<SquareCheckingAccuracy||
                        sqr_height < SquareCheckingAccuracy||
                        Math.Abs(sqr_width-sqr_height)>=SquareCheckingAccuracy)
                    {
                        ExitCheckingAction();
                        return;
                    }

                    squareMatrix =
                       new Color32
                       [sqr_height]
                       [];
                    for (i = 0; i < sqr_height; i++)
                    {
                        squareMatrix[i] = new Color32[sqr_width];
                        for (j = 0; j < sqr_width; j++)
                        {
                            squareMatrix[i][j] = 
                                imageMatrix[i + sqr_bottom][j + sqr_left];
                        }
                    }

                    //Check square deformation

                    State_ = AnchorChecker_State.SQRDeformationChecking;
                    {
                        bool CheckingCycle(Func<int, int, bool> checkFunc)
                        {
                            for (i = 0; i < SquareCheckingAccuracy; i++)
                            {
                                for (j = 0; j <= i; j++)
                                {
                                    if (checkFunc(i, j))
                                        return true;
                                }
                            }
                            return false;
                        }

                        bool CheckLeftBottom(int i, int j)
                        {
                            if (IsBlackPixel(squareMatrix[i][j]))
                                return true;
                            if (i != j && IsBlackPixel(squareMatrix[j][i]))
                                return true;
                            return false;
                        }
                        bool CheckRightBottom(int i, int j)
                        {
                            if (IsBlackPixel(squareMatrix[i][sqr_width - j - 1]))
                                return true;
                            if (i != j && IsBlackPixel(squareMatrix[sqr_height - j - 1][i]))
                                return true;
                            return false;
                        }
                        bool CheckLeftTop(int i, int j)
                        {
                            if (IsBlackPixel(squareMatrix[sqr_height - i - 1][j]))
                                return true;
                            if (i != j && IsBlackPixel(squareMatrix[j][sqr_width - i - 1]))
                                return true;
                            return false;
                        }
                        bool CheckRightTop(int i, int j)
                        {
                            if (IsBlackPixel(squareMatrix[sqr_height - i - 1][sqr_width - j - 1]))
                                return true;
                            if (i != j && IsBlackPixel(squareMatrix[sqr_height - j - 1][sqr_width - i - 1]))
                                return true;
                            return false;
                        }

                        if (!CheckingCycle(CheckLeftBottom) ||
                            !CheckingCycle(CheckRightBottom) ||
                            !CheckingCycle(CheckLeftTop) ||
                            !CheckingCycle(CheckRightTop))
                        {
                            ExitCheckingAction();
                            return;
                        }
                    }


                    //Find camera offset
                    float squareFitting =
                        (float)Math.Max(sqr_top - sqr_bottom, sqr_right - sqr_left) /
                        Math.Min(BG_Top - BG_Bottom, BG_Right - BG_Left);
                    CameraWorldPosition_ = new Vector3(0, 0, -squareFitting * SquareFitDistance);
                    State_ = AnchorChecker_State.FoundCameraWorldPosition;
                }
            }
            catch (Exception e)
            {
                var str= $"ANCHOR SCANNING ERROR\n" +
                    $"BG: {BG_Bottom} {BG_Top} {BG_Left} {BG_Right}\n" +
                    $"i={i} j={j}\n" +
                    $"BG_HorSidesSlicingStep={BG_HorSidesSlicingStep}\n" +
                    $"maxI={maxI} maxJ={maxJ}\n" +
                    $"ImageMatrix: {BGColumnsCount}x{BGRowsCount}\n" +
                    $"SquareBorders: {sqr_top} {sqr_bottom} {sqr_left} {sqr_right}\n" +
                    $"SquareMatrix: {sqr_width} {sqr_height}\n" +
                    $"State: {State_}\n" +
                    $"Exception: {e.Message}";
                ErrorEvent(str);
                 
            }
        }

        //Reserved variables for CheckAnchor
        private int BG_Bottom=-1, BG_Top=-1, BG_Left=-1, BG_Right=-1;
        private int i = 0, j = 0;
        private int BG_HorSidesSlicingStep = 0;
        private int maxI = 0,maxJ=0;
        private int BGColumnsCount=-1, BGRowsCount = -1;
        private int sqr_top = -1, sqr_bottom = -1, sqr_left = -1, sqr_right = -1;
        private int sqr_width = 0, sqr_height = 0;
        private Color32[][] imageMatrix;
        private Color32[][] squareMatrix;
    }
    private struct AnchorCheckingParam
    {
        public AnchorCheckingParam(Color32[] Pixels, int Width, int Height)
        {
            this.Pixels = Pixels;
            this.Width = Width;
            this.Height = Height;
        }
        public Color32[] Pixels;
        public int Width;
        public int Height;
    }


    public void StartChecking()
    {
        if (AnchorCheckingThread != null)
            return;
        bool supGyro = SystemInfo.supportsGyroscope;
        bool supAcc = SystemInfo.supportsAccelerometer;
        if(!(supGyro&&supAcc))
        {
            var message= (!supGyro ? "Gyroscope is not available on this device." : "") +
                (!supAcc ? "Accelerometer is not available on this device." : "");
            TempleMessagesManager.Instance_.CreateMessage(message, 1000000);
            return;
        }
        //NEED TO Change source of pixels to webcamtexture

        //Checker = new AnchorChecker(ColorThreshold, SquareCheckingAccuracy,SquareFitDistance);
        //AnchorCheckingParam param = new AnchorCheckingParam(TextureSource.Texture_.GetPixels32(),
        //    TextureSource.Texture_.width, TextureSource.Texture_.height);
        //Checker.AnchorCheckingStateChangedEvent += ChangeAnchorScanningStateAction;
        //Checker.CheckAnchor(param.Pixels, param.Width, param.Height);

        StartCoroutine(AnchorCheckingCycle());
    }
    public void StopChecking()
    {
        if (AnchorCheckingThread != null)
            AnchorCheckingThread.Abort();
    }

    private IEnumerator AnchorCheckingCycle()
    {
        string str = null;
        void CaughtErrorAction(string log) 
        { 
            str = log;
        }
        Checker = new AnchorChecker(ColorThreshold, SquareCheckingAccuracy,SquareFitDistance);
        Checker.AnchorCheckingStateChangedEvent += ChangeAnchorScanningStateAction;
        Checker.ErrorEvent += CaughtErrorAction;
        AutoResetEvent handler = new AutoResetEvent(true);
        AnchorCheckingParam param = new AnchorCheckingParam(TextureSource.Texture_.GetPixels32(),
            TextureSource.Texture_.width, TextureSource.Texture_.height);
        void RunChecking()
        {
            while (Checker.State_ != AnchorChecker.AnchorChecker_State.FoundCameraWorldPosition)
            {
                handler.WaitOne();
                handler.Reset();
                Checker.CheckAnchor(param.Pixels, param.Width, param.Height);
            }
        }
        AnchorCheckingThread = new Thread(new ThreadStart(RunChecking));
        AnchorCheckingThread.Start();
        while (true)
        {
            if (!string.IsNullOrEmpty(str))
            {
                DebugText.text = str;
                StopChecking();
            }
            if (Checker.State_ == AnchorChecker.AnchorChecker_State.FoundCameraWorldPosition)
            {
                CameraOffsetObject.transform.position = Checker.CameraWorldPosition_;
                StopChecking();
                FoundAnchorEvent(Checker.CameraWorldPosition_);
                GC.Collect();
                Destroy(this);
                yield break;
            }
            else if (Checker.State_ == AnchorChecker.AnchorChecker_State.Inactive)
            {
                param = new AnchorCheckingParam(TextureSource.Texture_.GetPixels32(),
                    TextureSource.Texture_.width, TextureSource.Texture_.height);
                handler.Set();
            }
            yield return null;
        }
    }
    private void ChangeAnchorScanningStateAction(AnchorChecker.AnchorChecker_State state)
    {
        ChangeAnchorScanningStateEvent.Invoke(state.ToString());
    }
}
