if (!TIMELINE.isMP)
{
   TIMELINE.lumenSetPageButtons = function(page, pageButtons)
   {
      var content = this.CONTENT;
      var header = content.lumenHeader;
      if (header == undefined || header.lumenPage != page)
      {
         header.removeMovieClip();
         header = content.createEmptyMovieClip("lumenHeader", content.getNextHighestDepth());
         header.lumenPage = page;
         header.onEnterFrame = function() { this._y = -this._parent._y; };
         header.beginFill(0x07372A);
         header.moveTo(0,0); header.lineTo(1280,0); header.lineTo(1280,64); header.lineTo(0,64); header.lineTo(0,0);
         header.endFill();
         header.beginFill(0xD1AD55);
         header.moveTo(0,61); header.lineTo(1280,61); header.lineTo(1280,64); header.lineTo(0,64); header.lineTo(0,61);
         header.endFill();
         this.lumenDrawLogo(header,"logo",95,8,42);
         header.createTextField("brand",header.getNextHighestDepth(),145,10,205,48);
         header.brand.multiline = true; header.brand.wordWrap = true; header.brand.selectable = false;
         header.brand.text = "Los Santos\nMedical Center";
         header.brand.setTextFormat(new TextFormat("$Font2",15,0xFFFFFF,true));
         var names = ["Início", "Pacientes", "Agenda"];
         var icons = ["home", "person", "calendar"];
         var destinations = ["WWW_LUMENSANTE_COM", "WWW_LUMENSANTE_COM_S_PACIENTES", "WWW_LUMENSANTE_COM_S_AGENDA"];
         var ids = ["LUMEN", "PACIENTES", "AGENDA"];
         header.lumenButtons = [];
         var browser = this.browser;
         for (var i = 0; i < names.length; i++)
         {
            var x = 402 + i * 155;
            if (ids[i] == page)
            {
               var active = header.createEmptyMovieClip("active" + i, header.getNextHighestDepth());
               active.beginFill(0x1A604C); active.moveTo(x,8); active.lineTo(x+146,8); active.lineTo(x+146,54);
               active.lineTo(x,54); active.lineTo(x,8); active.endFill();
            }
            this.lumenDrawIcon(header,"navIcon" + i,icons[i],x + 12,22,18,0xFFFFFF);
            var button = header.attachMovie("button_WOMtextLink","nav" + i,header.getNextHighestDepth(),{_x:x + 12,_y:21});
            button.btnTxt.text = "     " + names[i]; button.btnTxt.autoSize = "left";
            button.offColour = 0xFFFFFF; button.onColour = 0xE2BD69; button.destination = destinations[i];
            button.buttonEvent = function() { browser.GO_TO_WEBPAGE(this.destination); };
            header.lumenButtons.push(button.btnTxt);
         }
      }
      header._y = -content._y;
      header.swapDepths(content.getNextHighestDepth());
      this.browser.SET_PAGE_BUTTONS(header.lumenButtons.concat(pageButtons));
   };
}
