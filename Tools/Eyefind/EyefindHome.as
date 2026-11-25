// Appended to the first-frame script of the user's Eyefind movie.
if (!TIMELINE.isMP)
{
   TIMELINE.PAGE_NAMES[7] = "LUMEN";
   TIMELINE.CAN_STORE_PAGE[7] = true;
   TIMELINE.PAGE_NAMES[8] = "PACIENTES";
   TIMELINE.CAN_STORE_PAGE[8] = true;
   TIMELINE.PAGE_NAMES[9] = "AGENDA";
   TIMELINE.CAN_STORE_PAGE[9] = true;
   TIMELINE.displayPage = function(pageName)
   {
      this.CONTENT.gotoAndStop(pageName == "LUMEN" || pageName == "PACIENTES" || pageName == "AGENDA" ? "ERROR" : pageName);
      this.CONTENT.lumenLoading.removeMovieClip();
      if (pageName != "LUMEN" && pageName != "PACIENTES" && pageName != "AGENDA")
         this.CONTENT.lumenHeader.removeMovieClip();
      if (pageName != "LUMEN")
      {
         this.CONTENT.lumenHospital.removeMovieClip();
         this.CONTENT.bannerImage.removeMovieClip();
      }
      if (pageName != "PACIENTES")
      {
         this.CONTENT.lumenPatients.removeMovieClip();
      }
      if (pageName != "AGENDA")
         this.CONTENT.lumenAgenda.removeMovieClip();
   };

   TIMELINE.lumenShowLoading = function(title)
   {
      var content = this.CONTENT;
      if (content.lumenLoading == undefined)
      {
         var view = content.createEmptyMovieClip("lumenLoading", content.getNextHighestDepth());
         view.beginFill(0xEDF0ED);
         view.moveTo(0,0); view.lineTo(1280,0); view.lineTo(1280,740); view.lineTo(0,740); view.lineTo(0,0);
         view.endFill();
         view.beginFill(0xFFFFFF);
         view.moveTo(245,180); view.lineTo(1035,180); view.lineTo(1035,300); view.lineTo(245,300); view.lineTo(245,180);
         view.endFill();
         view.createTextField("message", view.getNextHighestDepth(), 270, 220, 740, 40);
         view.message.selectable = false;
         view.message.text = "Carregando informações do hospital...";
         view.message.setTextFormat(new TextFormat("$Font2", 22, 0x526E61, false));
      }
      content.BOUNDING_BOX._height = 740;
      this.browser.RESIZE_WEBSITE();
      var page = title.indexOf("Pacientes") >= 0 ? "PACIENTES" : title.indexOf("Agenda") >= 0 ? "AGENDA" : "LUMEN";
      this.lumenSetPageButtons(page, []);
      this.browser.SET_TITLEBAR_TEXT(title);
   };

   var lumenBrowserGo = TIMELINE.browser.GO_TO_WEBPAGE;
   var lumenAddressOriginal = TIMELINE.browser.updateAddressBarText;
   TIMELINE.browser.updateAddressBarText = function(address)
   {
      if (address == "WWW_EYEFIND_INFO_S_LUMEN")
      {
         address = "WWW_LOSSANTOSMEDICALCENTER_COM";
      }
      else if (address == "WWW_EYEFIND_INFO_S_PACIENTES")
      {
         address = "WWW_LOSSANTOSMEDICALCENTER_COM_S_PACIENTES";
      }
      else if (address == "WWW_EYEFIND_INFO_S_AGENDA")
         address = "WWW_LOSSANTOSMEDICALCENTER_COM_S_AGENDA";
      lumenAddressOriginal.call(this, address);
   };
   TIMELINE.browser.GO_TO_WEBPAGE = function(address)
   {
      if (address == "WWW_LUMENSANTE_COM" || address == "WWW_LOSSANTOSMEDICALCENTER_COM")
      {
         address = "WWW_EYEFIND_INFO_S_LUMEN";
      }
      else if (address == "WWW_LUMENSANTE_COM_S_PACIENTES" || address == "WWW_LOSSANTOSMEDICALCENTER_COM_S_PACIENTES")
      {
         address = "WWW_EYEFIND_INFO_S_PACIENTES";
      }
      else if (address == "WWW_LUMENSANTE_COM_S_AGENDA" || address == "WWW_LOSSANTOSMEDICALCENTER_COM_S_AGENDA")
         address = "WWW_EYEFIND_INFO_S_AGENDA";
      lumenBrowserGo.call(this, address);
   };

   var lumenSites = com.rockstargames.gtav.web.allWebsites.websitesArray;
   lumenSites.push(["LOSSANTOSMEDICALCENTER_COM", "hospital, los santos, medical center, saude, saúde"]);

   var lumenPopulateOriginal = TIMELINE.POPULATE_TEXT;
   TIMELINE.POPULATE_TEXT = function(pageName, searchArgs, newPage)
   {
      if (pageName == "AGENDA")
      {
         this.lumenRenderAgenda(newPage);
         return;
      }
      if (pageName == "PACIENTES")
      {
         this.lumenRenderPatients(newPage);
         return;
      }
      if (pageName == "LUMEN")
      {
         this.lumenRenderHome(newPage);
         return;
      }
      if (pageName == "SEARCH" && this.lumenHealthSearch)
      {
         this.lumenHealthSearch = false;
         this.searchResults.unshift("LOSSANTOSMEDICALCENTER_COM");
      }
      lumenPopulateOriginal.call(this, pageName, searchArgs, newPage);
      if (pageName != "PAGE1" || this.mcScope == undefined)
      {
         return;
      }

      var shortcut = this.mcScope.lumenHospitalShortcut;
      if (shortcut == undefined)
      {
         shortcut = this.mcScope.attachMovie("button_WOMtextLink", "lumenHospitalShortcut",
            this.mcScope.getNextHighestDepth(), {_x:520, _y:37});
         shortcut.buttonEvent = function()
         {
            _level0.TIMELINE.GO_TO_WEBPAGE("WWW_LUMENSANTE_COM");
         };
      }
      shortcut.btnTxt.text = "Los Santos Medical Center  >";
      shortcut.btnTxt.autoSize = "left";
      shortcut.offColour = 0xFFFFFF;
      shortcut.onColour = 0xFFCF32;
      this.dataTextScope.push(shortcut.btnTxt);
      this.browser.SET_PAGE_BUTTONS(this.dataTextScope);
   };

   var lumenAnchorOriginal = TIMELINE.goToAnchor;
   TIMELINE.goToAnchor = function(anchor)
   {
      this.lumenHealthSearch = anchor == "search_fashion";
      lumenAnchorOriginal.call(this, anchor);
   };
}
