if (!TIMELINE.isMP)
{
   TIMELINE.lumenCategory = 0;
   TIMELINE.lumenPage = 0;
   TIMELINE.lumenPatientId = "";
   TIMELINE.lumenRecordTab = 0;
   TIMELINE.lumenExamPage = 0;
   TIMELINE.lumenReport = -1;
   TIMELINE.lumenExamKind = -1;
   TIMELINE.lumenTreatmentSearch = "";
   TIMELINE.lumenExamSearch = "";
   TIMELINE.lumenOpenSequence = 0;

   var lumenHideKeyboard = TIMELINE.browser.HIDE_KEYBOARD;
   TIMELINE.browser.HIDE_KEYBOARD = function()
   {
      var target = this._currentSelectedTarget;
      var searchType = this.keyboardFocus != "" && target != undefined ? target.lumenSearchType : undefined;
      var query = searchType != undefined ? target.btnTxt.text : "";
      lumenHideKeyboard.call(this);
      if (searchType != undefined)
      {
         if (searchType == "treatment") TIMELINE.lumenTreatmentSearch = query;
         else if (searchType == "exam") TIMELINE.lumenExamSearch = query;
         TIMELINE.lumenRenderPatients(false);
      }
   };

   TIMELINE.lumenSendPatientAction = function(patientId, action)
   {
      var summary = this.dataProviderUI[97];
      if (summary == undefined || summary[0] != 2601 || summary[1] != 1) return;
      for (var index = 0; index < summary[4] && index < 4095; index++)
      {
         var entry = this.dataProviderUI[200 + index];
         if (entry != undefined && entry[1] == patientId)
         {
            this.browser.setFlags(18, action);
            this.browser.setFlags(19, summary[5] * 4096 + index + 1);
            return;
         }
      }
   };

   TIMELINE.lumenRenderPatients = function(newPage)
   {
      if (newPage)
      {
         this.lumenCategory = 0;
         this.lumenPage = 0;
         this.lumenPatientId = this.lumenAgendaPatientId == undefined ? "" : this.lumenAgendaPatientId;
         this.lumenAgendaPatientId = undefined;
         this.lumenRecordTab = 0;
         this.lumenExamPage = 0;
         this.lumenReport = -1;
         this.lumenExamKind = -1;
         this.lumenTreatmentSearch = "";
         this.lumenExamSearch = "";
      }

      var content = this.CONTENT;
      var openRequest = this.dataProviderUI[93];
      if (openRequest != undefined && openRequest[2] == 8 && openRequest[0] != this.lumenOpenSequence)
      {
         this.lumenOpenSequence = openRequest[0];
         this.lumenPatientId = openRequest[1] == undefined ? "" : openRequest[1];
      }
      if (newPage && this.lumenPatientId == "") this.browser.setFlags(20, 1);
      var summaryData = this.dataProviderUI[97];
      if (summaryData == undefined || summaryData[0] != 2601)
      {
         if (content.lumenPatients == undefined) this.lumenShowLoading("Los Santos Medical Center · Pacientes");
         return;
      }
      content.lumenLoading.removeMovieClip();
      var signature = this.lumenCategory + "|" + this.lumenPage + "|" + this.lumenPatientId +
         "|" + this.lumenRecordTab + "|" + this.lumenExamPage + "|" + this.lumenReport + "|" + this.lumenExamKind + "|" + this.lumenTreatmentSearch + "|" + this.lumenExamSearch;
      if (summaryData != undefined && summaryData[0] == 2601)
      {
         signature += "|" + summaryData.slice(0, 5).join(",");
         var signatureIndex = 0;
         while (signatureIndex < summaryData[4])
         {
            signature += "|" + String(this.dataProviderUI[200 + signatureIndex]);
            signatureIndex++;
         }
         var detailSignature = this.dataProviderUI[96];
         if (detailSignature != undefined && detailSignature[0] == this.lumenPatientId)
         {
            signature += "|" + String(detailSignature);
            for (var examSignature = 0; examSignature < detailSignature[1]; examSignature++)
               signature += "|" + String(this.dataProviderUI[1000 + examSignature]);
            for (var historySignature = 0; historySignature < detailSignature[4]; historySignature++)
               signature += "|" + String(this.dataProviderUI[2000 + historySignature]);
            for (var treatmentSignature = 0; treatmentSignature < detailSignature[5]; treatmentSignature++)
               signature += "|" + String(this.dataProviderUI[3000 + treatmentSignature]);
            for (var medicationSignature = 0; medicationSignature < detailSignature[6]; medicationSignature++)
               signature += "|" + String(this.dataProviderUI[4000 + medicationSignature]);
         }
         else if (this.lumenPatientId != "")
            this.lumenSendPatientAction(this.lumenPatientId, 1);
      }
      if (!newPage && content.lumenPatients != undefined && signature == this.lumenPatientsSignature)
         return;
      if (this.browser.keyboardFocus != "" && this.browser._currentSelectedTarget != undefined &&
          this.browser._currentSelectedTarget.lumenSearchType != undefined) return;
      this.lumenPatientsSignature = signature;
      content.lumenPatients.removeMovieClip();
      var view = content.createEmptyMovieClip("lumenPatients", content.getNextHighestDepth());
      view._x = 96;
      view._y = -36;
      view._xscale = 85;
      var browser = this.browser;
      var buttons = [];
      var site = this;

      function rectangle(x, y, width, height, color)
      {
         var shape = view.createEmptyMovieClip("shape" + view.getNextHighestDepth(), view.getNextHighestDepth());
         shape._x = x;
         shape._y = y;
         shape.beginFill(color);
         shape.moveTo(0, 0);
         shape.lineTo(width, 0);
         shape.lineTo(width, height);
         shape.lineTo(0, height);
         shape.lineTo(0, 0);
         shape.endFill();
         return shape;
      }
      function label(name, text, x, y, width, height, size, color, bold)
      {
         view.createTextField(name, view.getNextHighestDepth(), x, y, width, height);
         var field = view[name];
         field.selectable = false;
         field.multiline = true;
         field.wordWrap = true;
         field.text = text;
         field.setTextFormat(new TextFormat("$Font2", size, color, bold));
         field.autoSize = "left";
         return field;
      }
      function button(name, text, x, y, action, value)
      {
         var control = view.attachMovie("button_WOMtextLink", name, view.getNextHighestDepth(), {_x:x, _y:y});
         control.btnTxt.text = text;
         control.btnTxt.autoSize = "left";
         control.offColour = 0x17624D;
         control.onColour = 0xB48E45;
         control.lumenAction = action;
         control.lumenValue = value;
         control.buttonEvent = function()
         {
            if (this.lumenAction == "home")
               browser.GO_TO_WEBPAGE("WWW_LUMENSANTE_COM");
            else
            {
               if (this.lumenAction == "category")
               {
                  site.lumenCategory = this.lumenValue;
                  site.lumenPage = 0;
                  site.lumenPatientId = "";
               }
               else if (this.lumenAction == "page")
                  site.lumenPage = this.lumenValue;
               else if (this.lumenAction == "patient")
               {
                  site.lumenPatientId = this.lumenValue;
                  site.lumenRecordTab = 0;
                  site.lumenExamPage = 0;
                  site.lumenReport = -1;
                  site.lumenSendPatientAction(this.lumenValue, 1);
               }
               else if (this.lumenAction == "list")
               {
                  site.lumenPatientId = "";
                  browser.setFlags(20, 1);
               }
               else if (this.lumenAction == "call")
                  site.lumenSendPatientAction(site.lumenPatientId, 2);
               else if (this.lumenAction == "editNotes")
                  site.lumenSendPatientAction(site.lumenPatientId, 3);
               else if (this.lumenAction == "tab")
               {
                  site.lumenRecordTab = this.lumenValue;
                  site.lumenExamPage = 0;
                  site.lumenReport = -1;
               }
               else if (this.lumenAction == "examPage")
                  site.lumenExamPage = this.lumenValue;
               else if (this.lumenAction == "examKind")
                  site.lumenExamKind = this.lumenValue;
               else if (this.lumenAction == "clearSearch")
                  site.lumenTreatmentSearch = "";
               else if (this.lumenAction == "clearExamSearch")
                  site.lumenExamSearch = "";
               else if (this.lumenAction == "report")
                  site.lumenReport = this.lumenValue;
               else if (this.lumenAction == "request")
                  site.lumenSendPatientAction(site.lumenPatientId, 100 + this.lumenValue);
               else if (this.lumenAction == "medication")
                  site.lumenSendPatientAction(site.lumenPatientId, 300 + this.lumenValue);
               else if (this.lumenAction == "treatment")
                  site.lumenSendPatientAction(site.lumenPatientId, 400 + this.lumenValue);
               if (this.lumenAction == "category" || this.lumenAction == "patient" ||
                   this.lumenAction == "list" || this.lumenAction == "tab" || this.lumenAction == "report")
                  site.CONTENT._y = 0;
               site.lumenRenderPatients(false);
            }
         };
         buttons.push(control.btnTxt);
         return control;
      }
      function searchText(value)
      {
         var result = String(value).toLowerCase();
         var accents = ["á", "à", "â", "ã", "ä", "é", "ê", "í", "ó", "ô", "õ", "ú", "ü", "ç"];
         var plain = ["a", "a", "a", "a", "a", "e", "e", "i", "o", "o", "o", "u", "u", "c"];
         for (var a = 0; a < accents.length; a++) result = result.split(accents[a]).join(plain[a]);
         return result;
      }

      var fullX = -96 / 0.85;
      var fullWidth = 1280 / 0.85;
      var background = rectangle(fullX, 0, fullWidth, 740, 0xEDF0ED);
      var bottom = 740;
      button("home", "Início  ›", 176, 143, "home", 0);
      label("heading", "Pacientes", 176, 174, 800, 52, 30, 0x103A30, true);

      var summary = this.dataProviderUI[97];
      var ready = summary != undefined && summary[0] == 2601 && summary[1] == 1;
      var patients = [];
      var selected;
      var index = 0;
      if (ready)
      {
         while (index < summary[4])
         {
            var item = this.dataProviderUI[200 + index];
            if (item != undefined && item[1] != undefined && item[1] != "")
            {
               if (item[1] == this.lumenPatientId) selected = item;
               if (item[0] == this.lumenCategory) patients.push(item);
            }
            index++;
         }
      }

      if (!ready)
      {
         label("unavailable", "Inicie o plantão para acessar os pacientes.", 195, 270, 880, 50, 20, 0x526E61, false);
      }
      else if (this.lumenPatientId != "" && selected != undefined)
      {
         button("backList", "‹  Voltar aos pacientes", 176, 240, "list", 0);
         rectangle(176, 276, 928, 110, 0xFFFFFF);
         label("patientName", selected[2], 198, 286, 840, 38, 24, 0x103A30, true);
         label("patientStatus", selected[3] + " · " + selected[4], 198, 323, 800, 25, 15, 0x17624D, true);
         label("patientDetails", "Idade: " + selected[5] + " anos     •     Sangue: " + selected[6] +
            "     •     Telefone: " + selected[7], 198, 355, 860, 26, 14, 0x526E61, false);
         if (selected[11] == 1)
            button("callPatient", "Chamar paciente  ›", 785, 240, "call", selected[10]);
         var tabs = ["Exames", "Anotações", "Histórico", "Tratamentos", "Medicamentos", "Solicitar exames"];
         var tabIcons = ["exams", "notes", "history", "exams", "medicines", "exams"];
         for (index = 0; index < tabs.length; index++)
         {
            var tabX = 176 + (index % 3) * 310;
            var tabY = 397 + Math.floor(index / 3) * 43;
            rectangle(tabX, tabY, 302, 38, index == this.lumenRecordTab ? 0xDCEAE2 : 0xFFFFFF);
            this.lumenDrawIcon(view,"recordIcon" + index,tabIcons[index],tabX + 10,tabY + 10,18,0x17624D);
            button("recordTab" + index, "     " + tabs[index], tabX + 10, tabY + 8, "tab", index);
         }
         var detail = this.dataProviderUI[96];
         if (detail == undefined || detail[0] != selected[1])
            label("loading", "Carregando prontuário...", 195, 502, 850, 30, 17, 0x526E61, false);
         else if (this.lumenRecordTab == 1)
         {
            var notes = label("notes", detail[3] == "" ? "Nenhuma anotação." : detail[3], 195, 502, 880, 30, 16, 0x526E61, false);
            var notesEnd = notes._y + notes._height + 20;
            button("editNotes", "Editar anotações", 895, notesEnd, "editNotes", 0);
            bottom = Math.max(bottom, notesEnd + 55);
         }
         else if (this.lumenRecordTab == 2)
         {
            var historyY = 498;
            if (detail[4] == 0) label("historyEmpty", "Nenhum registro no histórico.", 195, historyY, 880, 30, 16, 0x526E61, false);
            for (index = 0; index < detail[4]; index++)
            {
               var history = this.dataProviderUI[2000 + index];
               if (history != undefined && history[0] == selected[1])
               {
                  var historyCard = rectangle(176, historyY, 928, 40, 0xFFFFFF);
                  var historyField = label("history" + index, history[1] + ": " + history[2], 195, historyY + 8, 875, 30, 15, 0x526E61, false);
                  var historyHeight = Math.max(40, historyField._height + 16);
                  historyCard._height = historyHeight;
                  historyY += historyHeight + 6;
               }
            }
            bottom = Math.max(bottom, historyY + 40);
         }
         else if (this.lumenRecordTab == 3 || this.lumenRecordTab == 4)
         {
            var isMedication = this.lumenRecordTab == 4;
            var careCount = isMedication ? detail[6] : detail[5];
            var careBase = isMedication ? 4000 : 3000;
            var careY = 492;
            if (!isMedication)
            {
               rectangle(176, careY, 928, 42, 0xFFFFFF);
               var search = button("treatmentSearch", this.lumenTreatmentSearch == "" ? "Pesquisar tratamento..." : this.lumenTreatmentSearch, 195, careY + 10, "search", 0);
               search.buttonType = 8;
               search.lumenSearchType = "treatment";
               search.btnTxt.autoSize = false;
               search.btnTxt._width = 600;
               search.buttonEvent = function() { browser.setKeyboard("TEXT_BOX"); };
               if (this.lumenTreatmentSearch != "") button("clearSearch", "Limpar", 895, careY + 10, "clearSearch", 0);
               careY += 52;
            }
            if (careCount == 0) label("careEmpty", "Nenhum tratamento disponível.", 195, careY, 850, 30, 16, 0x526E61, false);
            var careShown = 0;
            for (index = 0; index < careCount; index++)
            {
               var careItem = this.dataProviderUI[careBase + index];
               if (careItem != undefined && careItem[0] == selected[1] &&
                   (isMedication || this.lumenTreatmentSearch == "" || searchText(careItem[1]).indexOf(searchText(this.lumenTreatmentSearch)) >= 0))
               {
                  careShown++;
                  var careCard = rectangle(176, careY, 928, 38, careItem[2] == 1 ? 0xDCEFE2 : 0xFFFFFF);
                  var careName = label("careName" + index, careItem[1], 195, careY + 7, 670, 27, 15, 0x103A30, false);
                  if (detail[7] == 1)
                  {
                     if (isMedication)
                     {
                        var medButton = button("careAction" + index, careItem[2] == 1 ? "Remover" : "Receitar",
                           895, careY + 7, "medication", index * 2 + (careItem[2] == 1 ? 0 : 1));
                        if (careItem[2] == 1) medButton.offColour = 0xB33A36;
                     }
                     else
                        button("careAction" + index, careItem[2] == 1 ? "Selecionado" : "Selecionar", 895, careY + 7, "treatment", index);
                  }
                  else
                     label("careStatus" + index, careItem[2] == 1 ? "Ativo" : "—", 895, careY + 7, 180, 27, 14, 0x526E61, false);
                  var careHeight = Math.max(38, careName._height + 14);
                  careCard._height = careHeight;
                  careY += careHeight + 6;
               }
            }
            if (careCount > 0 && careShown == 0) label("careNoMatch", "Nenhum tratamento encontrado.", 195, careY, 850, 30, 16, 0x526E61, false);
            bottom = Math.max(bottom, careY + 40);
         }
         else
         {
            var exams = [];
            for (index = 0; index < detail[1]; index++)
            {
               var exam = this.dataProviderUI[1000 + index];
               if (exam != undefined && exam[0] == selected[1] &&
                   (this.lumenRecordTab == 5 || exam[2] == 4))
                  exams.push({row:exam, index:index});
            }
            if (this.lumenReport >= 0 && this.lumenRecordTab == 0)
            {
               var report = this.dataProviderUI[1000 + this.lumenReport];
               if (report != undefined && report[0] == selected[1] && report[2] == 4)
               {
                  button("reportBack", "‹ Voltar aos exames", 195, 490, "report", -1);
                  var reportName = label("reportName", report[1] + " · " + report[5], 195, 521, 880, 27, 16, 0x103A30, true);
                  var reportMethod = label("reportMethod", report[4] + " · " + report[6] + " · " + report[7], 195, reportName._y + reportName._height + 6, 880, 26, 14, 0x526E61, false);
                  var findings = label("reportFindings", report[3], 195, reportMethod._y + reportMethod._height + 8, 880, 30, 15, 0x526E61, false);
                  bottom = Math.max(bottom, findings._y + findings._height + 40);
               }
            }
            else
            {
               var examY = 492;
               if (this.lumenRecordTab == 5)
               {
                  rectangle(176, examY, 928, 42, 0xFFFFFF);
                  var examSearch = button("examSearch", this.lumenExamSearch == "" ? "Pesquisar exame..." : this.lumenExamSearch, 195, examY + 10, "search", 0);
                  examSearch.buttonType = 8;
                  examSearch.lumenSearchType = "exam";
                  examSearch.btnTxt.autoSize = false;
                  examSearch.btnTxt._width = 600;
                  examSearch.buttonEvent = function() { browser.setKeyboard("TEXT_BOX"); };
                  if (this.lumenExamSearch != "") button("clearExamSearch", "Limpar", 895, examY + 10, "clearExamSearch", 0);
                  examY += 52;
                  var kinds = ["Todos", "MRI", "Raio X", "Laboratoriais", "Outros"];
                  for (var kindIndex = 0; kindIndex < kinds.length; kindIndex++)
                  {
                     var kindX = 176 + kindIndex * 186;
                     rectangle(kindX, examY, 180, 34, this.lumenExamKind == kindIndex - 1 ? 0xDCEAE2 : 0xFFFFFF);
                     this.lumenDrawIcon(view,"kindIcon" + kindIndex,"exams",kindX + 8,examY + 9,16,0x17624D);
                     button("examKind" + kindIndex, "    " + kinds[kindIndex], kindX + 8, examY + 6, "examKind", kindIndex - 1);
                  }
                  examY += 44;
               }
               if (exams.length == 0) label("examEmpty", "Nenhum exame realizado.", 195, examY, 850, 30, 16, 0x526E61, false);
               var examsShown = 0;
               for (index = 0; index < exams.length; index++)
               {
                  var examItem = exams[index];
                  if (this.lumenRecordTab == 5 && this.lumenExamKind >= 0 && examItem.row[9] != this.lumenExamKind) continue;
                  if (this.lumenRecordTab == 5 && this.lumenExamSearch != "" && searchText(examItem.row[1]).indexOf(searchText(this.lumenExamSearch)) < 0) continue;
                  examsShown++;
                  var examCard = rectangle(176, examY, 928, 38, 0xFFFFFF);
                  var examName = label("examName" + index, examItem.row[1], 195, examY + 7, 600, 27, 15, 0x103A30, false);
                  if (this.lumenRecordTab == 0)
                     button("examReport" + index, "Ver laudo", 895, examY + 7, "report", examItem.index);
                  else if (detail[2] == 1 && examItem.row[2] == 0)
                     button("examRequest" + index, "Solicitar", 895, examY + 7, "request", examItem.index);
                  else
                     label("examStatus" + index, examItem.row[2] == 4 ? "Realizado" : examItem.row[2] == 0 ? "Indisponível" : "Aguardando",
                        895, examY + 7, 190, 26, 14, 0x526E61, false);
                  var examHeight = Math.max(38, examName._height + 14);
                  examCard._height = examHeight;
                  examY += examHeight + 6;
               }
               if (exams.length > 0 && examsShown == 0) label("examNoMatch", "Nenhum exame nesta categoria.", 195, examY, 850, 30, 16, 0x526E61, false);
               bottom = Math.max(bottom, examY + 40);
            }
         }
      }
      else
      {
         this.lumenPatientId = "";
         var categories = ["Aguardando", "Enfermaria", "Quartos", "Histórico"];
         var categoryIcons = ["person", "bed", "bed", "history"];
         for (index = 0; index < categories.length; index++)
         {
            var x = 176 + index * 225;
            rectangle(x, 231, 212, 43, index == this.lumenCategory ? 0xDCEAE2 : 0xFFFFFF);
            this.lumenDrawIcon(view,"categoryIcon" + index,categoryIcons[index],x + 12,243,18,0x17624D);
            button("category" + index, "     " + categories[index], x + 12, 242, "category", index);
         }
         if (patients.length == 0)
            label("empty", "Nenhum paciente nesta lista.", 195, 305, 840, 34, 19, 0x526E61, false);
         for (index = 0; index < patients.length; index++)
         {
            var patient = patients[index];
            var y = 287 + index * 48;
            rectangle(176, y, 928, 43, 0xFFFFFF);
            button("patient" + index, patient[2] + (patient[8] == 1 ? " · Agendado" : ""),
               194, y + 10, "patient", patient[1]);
            label("status" + index, patient[3] + " · " + patient[4], 560, y + 11, 510, 24, 14, 0x526E61, false);
         }
         bottom = Math.max(bottom, 287 + patients.length * 48 + 40);
      }
      background._height = bottom;
      content.BOUNDING_BOX._height = bottom - 36;
      browser.RESIZE_WEBSITE();
      content._y = Math.max(627 - bottom + 36, Math.min(0, content._y));
      this.lumenSetPageButtons("PACIENTES", buttons);
      view.lumenButtons = buttons;
      view.lumenCheckFrames = 180;
      view.onEnterFrame = function()
      {
         this.lumenCheckFrames--;
         if (this.lumenCheckFrames % 10 == 0)
         {
            var header = site.CONTENT.lumenHeader;
            var registered = browser.buttonManager.buttonMCList;
            var headerCount = header == undefined ? 0 : header.lumenButtons.length;
            if (header == undefined || registered == undefined || registered.length != headerCount + this.lumenButtons.length ||
                (headerCount > 0 && registered[0] != header.lumenButtons[0]._parent) ||
                (this.lumenButtons.length > 0 && registered[headerCount] != this.lumenButtons[0]._parent))
               site.lumenSetPageButtons("PACIENTES", this.lumenButtons);
         }
         if (this.lumenCheckFrames <= 0) delete this.onEnterFrame;
      };
      browser.SET_TITLEBAR_TEXT("Los Santos Medical Center · Pacientes");
   };
}
