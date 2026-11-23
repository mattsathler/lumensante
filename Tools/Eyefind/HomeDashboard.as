if (!TIMELINE.isMP)
{
   TIMELINE.lumenRenderHome = function(newPage)
   {
      var content = this.CONTENT;
      var data = this.dataProviderUI[97];
      if (data == undefined || data[0] != 2601)
      {
         this.lumenShowLoading("Los Santos Medical Center");
         return;
      }
      content.lumenLoading.removeMovieClip();
      var signature = data == undefined ? "" : data.slice(0, 5).join(",");
      var room = this.dataProviderUI[94];
      signature += "|" + String(room);
      if (data != undefined && data[0] == 2601)
         for (var j = 0; j < data[4]; j++) signature += "|" + String(this.dataProviderUI[200 + j]);
      if (!newPage && content.lumenHospital != undefined && this.lumenHomeSignature == signature) return;
      this.lumenHomeSignature = signature;
      content.lumenHospital.removeMovieClip();
      var view = content.createEmptyMovieClip("lumenHospital", content.getNextHighestDepth());
      view._x = 96;
      view._y = 64;
      view._xscale = 85;
      var site = this;
      var browser = this.browser;
      var buttons = [];
      function box(x, y, w, h, color)
      {
         var c = view.createEmptyMovieClip("box" + view.getNextHighestDepth(), view.getNextHighestDepth());
         c.beginFill(color); c.moveTo(x, y); c.lineTo(x + w, y); c.lineTo(x + w, y + h);
         c.lineTo(x, y + h); c.lineTo(x, y); c.endFill();
      }
      function text(name, value, x, y, w, h, size, color, bold)
      {
         view.createTextField(name, view.getNextHighestDepth(), x, y, w, h);
         var t = view[name]; t.selectable = false; t.multiline = true; t.wordWrap = true;
         t.text = value; t.setTextFormat(new TextFormat("$Font2", size, color, bold));
      }
      function link(name, value, x, y, destination, patient, light)
      {
         var c = view.attachMovie("button_WOMtextLink", name, view.getNextHighestDepth(), {_x:x, _y:y});
         c.btnTxt.text = value; c.btnTxt.autoSize = "left";
         c.offColour = light ? 0xFFFFFF : 0x17624D; c.onColour = 0xD1AD69;
         c.destination = destination; c.patient = patient;
         c.buttonEvent = function()
         {
            if (this.patient != undefined) site.lumenAgendaPatientId = this.patient;
            browser.GO_TO_WEBPAGE(this.destination);
         };
         buttons.push(c.btnTxt);
      }
      function icon(x, y, kind, color)
      {
         var c = view.createEmptyMovieClip("icon" + view.getNextHighestDepth(), view.getNextHighestDepth());
         c._x = x / 2 + 6;
         c._y = y / 2 + 6;
         c._xscale = 50;
         c._yscale = 50;
         c.lineStyle(2, color, 100);
         if (kind == "cross")
         {
            c.moveTo(x + 9,y); c.lineTo(x + 17,y); c.lineTo(x + 17,y + 8); c.lineTo(x + 25,y + 8);
            c.lineTo(x + 25,y + 16); c.lineTo(x + 17,y + 16); c.lineTo(x + 17,y + 24);
            c.lineTo(x + 9,y + 24); c.lineTo(x + 9,y + 16); c.lineTo(x + 1,y + 16);
            c.lineTo(x + 1,y + 8); c.lineTo(x + 9,y + 8); c.lineTo(x + 9,y);
         }
         else if (kind == "calendar")
         {
            c.moveTo(x,y+4); c.lineTo(x+24,y+4); c.lineTo(x+24,y+24); c.lineTo(x,y+24); c.lineTo(x,y+4);
            c.moveTo(x,y+10); c.lineTo(x+24,y+10); c.moveTo(x+6,y); c.lineTo(x+6,y+7);
            c.moveTo(x+18,y); c.lineTo(x+18,y+7); c.moveTo(x+8,y+16); c.lineTo(x+16,y+16);
         }
         else
         {
            c.moveTo(x+8,y+4); c.lineTo(x+16,y+4); c.lineTo(x+19,y+8); c.lineTo(x+19,y+14);
            c.lineTo(x+16,y+18); c.lineTo(x+8,y+18); c.lineTo(x+5,y+14); c.lineTo(x+5,y+8); c.lineTo(x+8,y+4);
            c.moveTo(x+1,y+25); c.lineTo(x+1,y+22); c.lineTo(x+7,y+19); c.moveTo(x+17,y+19); c.lineTo(x+23,y+22); c.lineTo(x+23,y+25);
         }
      }
      var ready = data != undefined && data[0] == 2601 && data[1] == 1;
      box(-96 / 0.85,0,1280 / 0.85,780,0xEDF0ED);
      text("top","CENTRAL DO HOSPITAL",176,20,530,26,14,0x103A30,true);
      text("live",ready ? "Plantão em andamento" : "Plantão encerrado",878,20,225,26,13,0x17624D,true);
      box(176,57,928,180,0x073D2E);
      box(176,57,6,180,0xD1AD55);
      text("heroLabel","CENTRAL DE ATENDIMENTO",198,79,440,25,13,0xE8C25F,true);
      text("heroTitle","Cuidar começa aqui.",198,111,480,48,30,0xFFFFFF,true);
      text("heroBody","Pacientes, prontuários e agenda do Los Santos Medical Center.",198,169,425,48,16,0xE1EEE8,false);
      box(176,253,454,68,0xDCEAE2); box(644,253,460,68,0xFFFFFF);
      icon(196,275,"person",0x17624D); icon(664,275,"calendar",0x17624D);
      text("patientCount","Pacientes   " + (ready ? data[2] : "—"),235,271,360,35,21,0x103A30,true);
      text("waitingCount","Em espera   " + (ready ? data[3] : "—"),703,271,360,35,21,0x103A30,true);
      box(176,337,562,322,0xFFFFFF);
      text("listTitle","Pacientes do plantão",196,353,380,32,20,0x103A30,true);
      link("allPatients","Ver todos  ›",615,356,"WWW_LUMENSANTE_COM_S_PACIENTES",undefined,false);
      box(752,337,352,140,0xFFFFFF);
      text("roomTitle","Sala de diagnóstico",772,353,305,30,18,0x103A30,true);
      text("roomDetails",ready && room != undefined && room[0] == 1 ?
         room[1] + "  ·  " + (room[2] == 1 ? "Ocupada" : "Disponível") +
         (room[2] == 1 && room[3] != "" ? "\n" + room[3] : "") : "Disponível durante o plantão",
         772,393,300,65,14,0x526E61,false);
      var shown = 0;
      if (ready)
      {
         for (j = 0; j < data[4] && shown < 4; j++)
         {
            var row = this.dataProviderUI[200 + j];
            if (row != undefined && row[0] != 3)
            {
               var y = 394 + shown * 54;
               box(195,y,525,47,shown % 2 == 0 ? 0xEDF4EE : 0xF7F8F6);
               link("patient" + j,row[2],207,y + 4,"WWW_LUMENSANTE_COM_S_PACIENTES",row[1],false);
               text("state" + j,row[3] + " · " + row[4],207,y + 24,490,20,12,0x526E61,false);
               shown++;
            }
         }
      }
      if (shown == 0) text("empty",ready ? "Nenhum paciente disponível." : "Inicie o plantão para acessar os pacientes.",207,404,480,55,16,0x526E61,false);
      box(752,493,352,70,0xFFFFFF);
      icon(770,515,"calendar",0x17624D);
      link("agendaLink","Abrir agenda  ›",806,514,"WWW_LUMENSANTE_COM_S_AGENDA",undefined,false);
      var banner = content.bannerImage;
      if (banner == undefined) banner = this.lumenDrawBanner(content, 96 + 674 * 0.85, 64 + 57, 430 * 0.85, 180);
      banner.swapDepths(content.getNextHighestDepth());
      content.BOUNDING_BOX._height = 844;
      browser.RESIZE_WEBSITE();
      content._y = Math.max(-217, Math.min(0, content._y));
      this.lumenSetPageButtons("LUMEN", buttons);
      browser.SET_TITLEBAR_TEXT("Los Santos Medical Center");
   };
}
