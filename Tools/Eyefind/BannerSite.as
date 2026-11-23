if (!TIMELINE.isMP)
{
   TIMELINE.lumenBannerPixels = "__LUMEN_BANNER_PIXELS__";
   TIMELINE.lumenDrawBanner = function(parent, x, y, width, height)
   {
      var clip = parent.createEmptyMovieClip("bannerImage", parent.getNextHighestDepth());
      clip._x = x; clip._y = y;
      var pixels = this.lumenBannerPixels;
      var pixelWidth = width / 128;
      var pixelHeight = height / 48;
      for (var row = 0; row < 48; row++)
      {
         var column = 0;
         while (column < 128)
         {
            var pixel = pixels.substr((row * 128 + column) * 3, 3);
            var end = column + 1;
            while (end < 128 && pixels.substr((row * 128 + end) * 3, 3) == pixel) end++;
            var red = parseInt(pixel.substr(0, 1), 16) * 17;
            var green = parseInt(pixel.substr(1, 1), 16) * 17;
            var blue = parseInt(pixel.substr(2, 1), 16) * 17;
            clip.beginFill(red * 65536 + green * 256 + blue);
            clip.moveTo(column * pixelWidth, row * pixelHeight);
            clip.lineTo(end * pixelWidth, row * pixelHeight);
            clip.lineTo(end * pixelWidth, (row + 1) * pixelHeight);
            clip.lineTo(column * pixelWidth, (row + 1) * pixelHeight);
            clip.lineTo(column * pixelWidth, row * pixelHeight);
            clip.endFill();
            column = end;
         }
      }
      return clip;
   };
}
