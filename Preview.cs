using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace
{
    internal class Preview
    {
        private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        public int timerInterval;
        private int currentAnimationIndex;
        private int currentFPSTarget = 12;
        private bool playAnimation = true;
        private List<Image> layerPreviewImages;
        private List<List<WriteableBitmap>> previewLayers;

        public void Initialization(List<List<WriteableBitmap>> layers, List<Image> images) 
        {
            previewLayers = layers;
            layerPreviewImages = images;
            timerInterval = 1000 / currentFPSTarget;
            timer.Interval = timerInterval;
            timer.Enabled = true;
            timer.Tick += new EventHandler(OnTimedEvent);
        }

        public void Update(List<List<WriteableBitmap>> layers, int currentBitmapIndex) 
        {
            previewLayers = layers;
            if (playAnimation == false || currentFPSTarget == 0)
            {
                currentAnimationIndex = currentBitmapIndex;
                SetPreviewAnimationImages(currentAnimationIndex);
            }
        }

        private void OnTimedEvent(object sender, EventArgs e)
        {
            if (currentAnimationIndex + 1 < previewLayers[0].Count) currentAnimationIndex += 1;
            else currentAnimationIndex = 0;
            SetPreviewAnimationImages(currentAnimationIndex);
        }

        public void SetPreviewAnimationImages(int animationIndex)
        {
            for (int i = 0; i < layerPreviewImages.Count; i++)
            {
                if (i < previewLayers.Count)
                {
                    if (animationIndex < previewLayers[i].Count && animationIndex >= 0)
                    {
                        layerPreviewImages[i].Source = previewLayers[i][animationIndex];
                    }
                    else 
                    {
                        layerPreviewImages[i].Source = null;
                    }
                }
                else 
                {
                    layerPreviewImages[i].Source = null;
                } 
            }
        }

        public void FramesPerSecond(int FPSTarget, int currentBitmapIndex)
        {
            currentFPSTarget = FPSTarget;
            if (currentFPSTarget != 0)
            {
                timerInterval = 1000 / currentFPSTarget;
                timer.Stop();
                timer.Interval = timerInterval;
                PlayAnimation();
            }
            else
            {
                StopAnimation(currentBitmapIndex);
            }
        }

        public bool Play(int currentBitmapIndex)
        {
            if (playAnimation == true)
            {
                playAnimation = false;
                StopAnimation(currentBitmapIndex);
            }
            else
            {

                playAnimation = true;
                if (currentFPSTarget != 0)
                {
                    timerInterval = 1000 / currentFPSTarget;
                    timer.Interval = timerInterval;
                    timer.Start();
                }
            }
            return playAnimation;
        }

        public void StopAnimation(int currentBitmapIndex)
        {
            timer.Stop();
            currentAnimationIndex = currentBitmapIndex;
            SetPreviewAnimationImages(currentAnimationIndex);
        }

        public void PlayAnimation()
        {
            if (playAnimation == true) timer.Start();
        }
    }
}
