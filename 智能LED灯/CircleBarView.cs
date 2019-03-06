using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;

namespace SLED
{
    public class CircleBarView : View
    {
        CircleBarAnim anim;
        private RectF mRectF = new RectF();//绘制圆弧的矩形区域

        private Color progressColor;//进度条圆弧颜色
        private Color bgColor;//背景圆弧颜色
        private float startAngle;//背景圆弧的起始角度
        private float sweepAngle;//背景圆弧扫过的角度
        private float barWidth;//圆弧进度条宽度

        private int defaultSize;//自定义View默认的宽高

        private Paint bgPaint;//绘制背景圆弧的画笔

        private float progressNum;//可以更新的进度条数值
        private float maxNum;//进度条最大值

        private float NowProgressNum;//最近一次的进度
        private float progressSweepAngle;//进度条圆弧扫过的角度

        private Paint progressPaint;//绘制圆弧的画笔
        public CircleBarView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Init(context, attrs);
        }
        
        public static int Dip2px(Context context, float dpValue)
        {
            float scale = context.Resources.DisplayMetrics.Density;
            return (int)(dpValue * scale + 0.5f);
        }
        private void Init(Context context, IAttributeSet attrs)
        {
            anim = new CircleBarAnim(this);

            defaultSize = DpOrPxUtils.dip2px(context, 100);
            barWidth = DpOrPxUtils.dip2px(context, 10);
            
            TypedArray typedArray = context.ObtainStyledAttributes(attrs, Resource.Styleable.CircleBarView);

            progressColor = typedArray.GetColor(Resource.Styleable.CircleBarView_progress_color, Color.Green);//默认为绿色
            bgColor = typedArray.GetColor(Resource.Styleable.CircleBarView_bg_color, Color.Gray);//默认为灰色
            startAngle = typedArray.GetFloat(Resource.Styleable.CircleBarView_start_angle, 0);//默认为0
            sweepAngle = typedArray.GetFloat(Resource.Styleable.CircleBarView_sweep_angle, 360);//默认为360
            barWidth = typedArray.GetDimension(Resource.Styleable.CircleBarView_bar_width, DpOrPxUtils.dip2px(context, 10));//默认为10dp
            typedArray.Recycle();//typedArray用完之后需要回收，防止内存泄漏
            

            progressPaint = new Paint();
            progressPaint.StrokeCap= Paint.Cap.Round;
            progressPaint.SetStyle(Paint.Style.Stroke);//只描边，不填充
            progressPaint.Color = progressColor;
            progressPaint.AntiAlias = true;//设置抗锯齿
            progressPaint.StrokeWidth = barWidth;//随便设置一个画笔宽度，看看效果就好，之后会通过attr自定义属性进行设置

            bgPaint = new Paint();
            bgPaint.StrokeCap = Paint.Cap.Round;
            bgPaint.SetStyle(Paint.Style.Stroke);//只描边，不填充
            bgPaint.Color = bgColor;
            bgPaint.AntiAlias = true;//设置抗锯齿
            bgPaint.StrokeWidth = barWidth;

            NowProgressNum = 0;
            progressNum = 0;
            maxNum = 100;//也是随便设的
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
            canvas.DrawArc(mRectF, startAngle, sweepAngle, false, bgPaint);
            canvas.DrawArc(mRectF, startAngle, progressSweepAngle, false, progressPaint);
        }
        public class CircleBarAnim : Animation
        {
            CircleBarView View;
            public CircleBarAnim(CircleBarView View)
            {
                this.View = View;
            }
            protected override void ApplyTransformation(float interpolatedTime, Transformation t)
            {
                base.ApplyTransformation(interpolatedTime, t);
                View.progressPaint.Color = ColorGradient(Color.Yellow,Color.Red,(View.NowProgressNum + (View.progressNum - View.NowProgressNum) * interpolatedTime)/ View.maxNum);
                View.progressSweepAngle =( View.sweepAngle * View.NowProgressNum / View.maxNum)+((View.progressNum- View.NowProgressNum) / View.maxNum)* View.sweepAngle * interpolatedTime;//这里计算进度条的比例
                View.PostInvalidate();
                if (interpolatedTime == 1) View.NowProgressNum = View.progressNum;
            }
            private Color ColorGradient(Color StartColor ,Color EndColor,float Percentage)
            {

                int redSpace = EndColor.R - StartColor.R;
                int greenSpace = EndColor.G - StartColor.G;
                int blueSpace = EndColor.B - StartColor.B;

                return new Color(StartColor.R + (int)(Percentage * redSpace), StartColor.G + (int)(Percentage * greenSpace), StartColor.B + (int)(Percentage * blueSpace));
            }
        }
        public void SetProgressNum(int time, int progressNum)
        {
            NowProgressNum = this.progressNum;
            this.progressNum = progressNum;
            anim.Duration = time;
            StartAnimation(anim);
        }


        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            int height = MeasureSpec.GetSize(heightMeasureSpec);
            int width = MeasureSpec.GetSize(widthMeasureSpec);
            int min = Math.Min(width, height);// 获取View最短边的长度
            SetMeasuredDimension(min, min);// 强制改View为以最短边为长度的正方形

            if (min >= barWidth * 2)
            {//这里简单限制了圆弧的最大宽度
                mRectF.Set(barWidth / 2, barWidth / 2, min - barWidth / 2, min - barWidth / 2);
            }

        }
    }
    public class DpOrPxUtils
    {
        public static int dip2px(Context context, float dpValue)
        {
            float scale = context.Resources.DisplayMetrics.Density;
            return (int)(dpValue * scale + 0.5f);
        }
        public static int px2dip(Context context, float pxValue)
        {
            float scale = context.Resources.DisplayMetrics.Density;
            return (int)(pxValue / scale + 0.5f);
        }
    }
}