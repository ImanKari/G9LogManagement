// Save Charts With Name - ChartsItems[["Name", Object]]
var ChartsItems = [];
// Save Creator Functions
var ChartsCreatorFunc = [];
// Array for save data logs
/* JavaScript G9DataLog Format:
* Array [
* 0 => byte logType,
* 1 => string identity,
* 2 => string ({title}🅖➒{body}🅖➒{path}🅖➒{method}🅖➒{line}), // Encrypt this column if encryption enable
* 3 => string logDateTime:yyyy/MM/dd hh:mm:ss
* 4 => int ItemNumber Add programmatically run time
* ]
* 🅖➒ => Code => "\ud83c\udd56\u2792"
*/
var G9DataLog = [];
// Array for save decoded log index
var G9DataLogDecoded = [];
// Fields save log type counts
var CountEvent, CountInfo, CountWarning, CountError, CountException;
CountEvent = CountInfo = CountWarning = CountError = CountException = 0;
// Fields for save log percents
var PercentEvent, PercentInfo, PercentWarning, PercentError, PercentException;
PercentEvent = PercentInfo = PercentWarning = PercentError = PercentException = 0;
// Array for save logs statistics in time
// Format: Statistics....[[int hour, int count], [int hour, int count], [int hour, int count], ...]
var StatisticsEvent = [], StatisticsInfo = [], StatisticsWarning = [], StatisticsError = [], StatisticsException = [];
// Data statistics for chart 2
var eventStatisticsDataPoint = [],
    infoStatisticsDataPoint = [],
    warnStatisticsDataPoint = [],
    errorStatisticsDataPoint = [],
    excepStatisticsDataPoint = [];
// Data average for chart 3
var aveHour = 0, aveMinute = 0, aveSecond = 0;
// Array for save min and max time
// MinAndMaxHourAndMinutesTime = array(int startRange, int endRange)
var MinAndMaxHourAndMinutesTime = [];
// Set default value for Statistics.... array
for (var i = 0; i < 24; i++) {
    StatisticsEvent[i] = 0;
    StatisticsInfo[i] = 0;
    StatisticsWarning[i] = 0;
    StatisticsError[i] = 0;
    StatisticsException[i] = 0;
}

$(document).ready(function() {

    // Load all setting script
    function AddConfigFile() {
        try {
            var script = document.createElement("script");
            script.onload = function() {
                AddAllSettingScript();
            };
            script.src = `Data/G9Config.js?v${Math.random()}`;

            document.getElementsByTagName("head")[0].appendChild(script);

        } catch (error) {
            console.log(error);
        }
    }

    // Index of setting file
    var settingFileCount = 0;
    // field for timeout
    var timeOutHandler;

    // Load all setting script
    function AddAllSettingScript() {
        try {
            var script = document.createElement("script");
            script.onload = function() {
                settingFileCount++;
                AddAllSettingScript();
            };
            script.src = `Data/${settingFileCount}-G9Setting.js?v${Math.random()}`;

            document.getElementsByTagName("head")[0].appendChild(script);

            clearTimeout(timeOutHandler);
            timeOutHandler = setTimeout(function() {
                    OnLoadTotalSettingScriptFile();
                    // Load data log
                    AddAllDataLogScript();
                },
                369);
        } catch (error) {
            console.log(error);
        }
    }

    // When load all setting script
    function OnLoadTotalSettingScriptFile() {

        $("#ContentForShowLogFile_ShowFile").html("");
        $("#ContentForShowLogFile_ShowFile").append(
            `<div id="G9OpenAllFiles" class="col-xl-3 col-lg-4 col-md-6 col-sm-11 ContentForShowLogFile_SameFixedHeight">
                    <div style="height: 194px;" class="col-12 ContentForShowLogFile_ShowFile_File card mb-3 bg-arielle-smile widget-chart text-white card-border">
                        <div class="ContentForShowLogFile_ShowFile_File_InfoItems">
                            <div class="ContentForShowLogFile_ShowFile_File_InfoItems_Special">
                                <div class="ContentForShowLogFile_ShowFile_File_InfoItems_SpecialText"
                                                        g9-bind-value="OpenAllFiles">${GetResourceLanguage(
                "OpenAllFiles")}</div>
                                </ br>
                                <div class="ContentForShowLogFile_ShowFile_File_InfoItems_SpecialIcon" >
                                    <img src="Utilities/ICON/DashboardIcon/LogFiles.png" />
                                </div>
                            </div>
                        </div>
                    </div>
                </div>`
        );

        for (let i = G9StartDateTime.length - 1; i >= 0; i--) {
            AddNewFileInfo(
                G9StartDateTime[i] === "" ? "NONE" : G9StartDateTime[i],
                G9FinishDateTime[i] === "" ? "NONE" : G9FinishDateTime[i],
                G9FileSize[i] === "" ? "NONE" : G9FileSize[i],
                G9FileCloseReason[i] === "" ? "NONE" : G9FileCloseReason[i]
            );
        }
    }

    // Load Config And Settings
    AddConfigFile();

    // Decoding data
    function Decoding(text, key, iv) {
        try {
            key = CryptoJS.enc.Utf8.parse(key);
            iv = CryptoJS.enc.Utf8.parse(iv);
            var decrypted = CryptoJS.AES.decrypt(text,
                key,
                {
                    keySize: 128 / 8,
                    iv: iv,
                    mode: CryptoJS.mode.CBC,
                    padding: CryptoJS.pad.Pkcs7
                });
            return decrypted.toString(CryptoJS.enc.Utf8);
        } catch (err) {
            console.log(err);
            return "";
        }
    }

    // Add new file info in grid
    function AddNewFileInfo(startDateTime, finishDateTime, fileSize, closeReason) {

        var defaultFileShowContent = `
        <div class="col-xl-3 col-lg-4 col-md-6 col-sm-11 ContentForShowLogFile_SameFixedHeight">
                    <div class="col-12 ContentForShowLogFile_ShowFile_File card mb-3 bg-arielle-smile widget-chart text-white card-border">
                        <div class="ContentForShowLogFile_ShowFile_File_InfoItems">
                            <div class="ContentForShowLogFile_ShowFile_File_InfoItems_Header" g9-bind-value="FileSize" >${
            GetResourceLanguage("FileSize")}</div>
                            <div class="ContentForShowLogFile_ShowFile_File_InfoItems_Body">${fileSize !== "NONE"
            ? fileSize + ` <a g9-bind-value="Kilobyte">` + GetResourceLanguage("Kilobyte") + `</a>`
            : `<a g9-bind-value="Unknown">${GetResourceLanguage("Unknown")}</a>`}</div>
                            <div class="ContentForShowLogFile_ShowFile_File_InfoItems_Header" g9-bind-value="StartDateTime">${
            GetResourceLanguage("StartDateTime")}</div>
                            <div class="ContentForShowLogFile_ShowFile_File_InfoItems_Body">${startDateTime}</div>
                            <div class="ContentForShowLogFile_ShowFile_File_InfoItems_Header" g9-bind-value="EndDateTime">${
            GetResourceLanguage("EndDateTime")}</div>
                            <div class="ContentForShowLogFile_ShowFile_File_InfoItems_Body">${finishDateTime}</div>
                            <div class="ContentForShowLogFile_ShowFile_File_InfoItems_Header" g9-bind-value="CloseReason">${
            GetResourceLanguage("CloseReason")}</div>
                            <div class="ContentForShowLogFile_ShowFile_File_InfoItems_Body" g9-bind-value="${
            closeReason}">${GetResourceLanguage(closeReason)}</div>
                        </div>
                    </div>
                </div>
        `;

        $("#ContentForShowLogFile_ShowFile").append(defaultFileShowContent);
    }

    // Specify initialize data logs and fill grid for show data logs
    var _initializeFirstTimeDataLogs = false;

    // Handler click for open log data files
    $(document).on("click",
        "#G9OpenAllFiles",
        function() {
            HandleShowLogDataFilesGrid();
        });

    // Handle show log data files and fill grid
    function HandleShowLogDataFilesGrid() {
        $("#UserLoginInfo").fadeOut(369);
        setTimeout(function() {
                $(".sidebar-search").fadeIn(369);
            },
            369);

        $("#PanelInformation, #ContentForShowLogFile").fadeOut(369);
        setTimeout(function() {
                $("#ContentShowLog").css("display", "");
                $("#ContentShowLog").fadeOut(0);
                $("#ContentShowLog").fadeIn(369);
                // Set initialize for first time and return if initialized data logs
                if (!_initializeFirstTimeDataLogs) {
                    _initializeFirstTimeDataLogs = true;
                    // Set filtered data
                    SetFilterOnG9DataLog();
                }
            },
            369);

        setTimeout(function() {
                // Set item menu number
                _specifyItemMenuActive = 1;
            },
            639);
    }


    $(".sidebar-dropdown > a").click(function() {
        $(".sidebar-submenu").slideUp(200);
        if (
            $(this)
                .parent()
                .hasClass("active")
        ) {
            $(".sidebar-dropdown").removeClass("active");
            $(this)
                .parent()
                .removeClass("active");
        } else {
            $(".sidebar-dropdown").removeClass("active");
            $(this)
                .next(".sidebar-submenu")
                .slideDown(200);
            $(this)
                .parent()
                .addClass("active");
        }
    });

    $("#close-sidebar").click(function() {
        $(".page-wrapper").removeClass("toggled");
        setTimeout(function() {
                RefreshAllChart();
            },
            199);
    });
    $("#show-sidebar").click(function() {
        $(".page-wrapper").addClass("toggled");
        setTimeout(function() {
                RefreshAllChart();
            },
            199);
    });


    $(".toggle_fullscreen").on("click",
        function() {
            // if already full screen; exit
            // else go fullscreen
            if (
                document.fullscreenElement ||
                    document.webkitFullscreenElement ||
                    document.mozFullScreenElement ||
                    document.msFullscreenElement
            ) {
                if (document.exitFullscreen) {
                    document.exitFullscreen();
                } else if (document.mozCancelFullScreen) {
                    document.mozCancelFullScreen();
                } else if (document.webkitExitFullscreen) {
                    document.webkitExitFullscreen();
                } else if (document.msExitFullscreen) {
                    document.msExitFullscreen();
                }
            } else {
                var button = $(this);
                var chartId = $(this).parent().attr("ChartId");

                var element = $(this).parent().get(0);


                if (element.requestFullscreen) {
                    element.requestFullscreen();
                } else if (element.mozRequestFullScreen) {
                    element.mozRequestFullScreen();
                } else if (element.webkitRequestFullscreen) {
                    element.webkitRequestFullscreen(Element.ALLOW_KEYBOARD_INPUT);
                } else if (element.msRequestFullscreen) {
                    element.msRequestFullscreen();
                } else {
                    alert("FullScreen not supported!");
                }

                $(element).bind("webkitfullscreenchange mozfullscreenchange fullscreenchange",
                    function(e) {
                        var state = document.fullScreen || document.mozFullScreen || document.webkitIsFullScreen;
                        var event = state ? "FullscreenOn" : "FullscreenOff";
                        var selectedChart = ChartsItems[chartId];
                        // Set requirement when full screen on
                        if (event === "FullscreenOn") {
                            selectedChart.set("exportEnabled", true);
                            for (let i = 0; i < selectedChart.options.data.length; i++) {
                                selectedChart.options.data[i].showInLegend = true;
                            }
                            $(button).css("height", "39.9px");
                            $(element).css("padding-bottom", "43.9px");
                            $(element).css("padding-top", "13.9px");
                        }
                        // Set requirement when full screen off
                        else {
                            selectedChart.set("exportEnabled", false);
                            for (let j = 0; j < selectedChart.options.data.length; j++) {
                                selectedChart.options.data[j].showInLegend = false;
                            }
                            $(button).css("height", "");
                            $(element).css("padding-bottom", "");
                            $(element).css("padding-top", "");

                            // Unbind when exit from full screen
                            $(element).unbind("webkitfullscreenchange mozfullscreenchange fullscreenchange");
                        }

                        selectedChart.render();
                    });
            }
        });


    // ##################### Chart Handler #####################
    //Chart Total Log
    var chartTotal;
    ChartsCreatorFunc[0] = function CreateChartForTotalLog() {
        chartTotal = new CanvasJS.Chart("ChartForTotalLog",
            {
                backgroundColor: "",
                animationEnabled: true,
                animationDuration: 639,
                title: {
                    text: GetResourceLanguage("LogStatistics"),
                    fontColor: "#eee"
                },
                legend: {
                    cursor: "pointer",
                    itemclick: toggleLegendItems,
                    fontColor: "#eee"
                },
                toolTip: {
                    enabled: true,
                    animationEnabled: true,
                    shared: true,
                    contentFormatter: function(e) {
                        var str = "";
                        for (let i = 0; i < e.entries.length; i++) {
                            var temp =
                                `<b><a style='color: ${e.entries[i].dataPoint.color}'>${e.entries[i].dataPoint.label
                                    }: ${e.entries[i].dataPoint.y} ${GetResourceLanguage("Percentage")}</a></b>`;
                            str = str.concat(temp);
                        }
                        return (str);
                    }
                },
                data: [
                    {
                        type: "pie",

                        indexLabelFontColor: "#fff",
                        indexLabelFontSize: 12,
                        indexLabelPlacement: "inside",
                        indexLabelFontWeight: "bold",
                        yValueFormatString: "##0.00\"%\"",
                        indexLabel: "{label}",
                        dataPoints: [
                            { y: PercentEvent.toFixed(2), label: GetResourceLanguage("Event"), color: "#49c162" },
                            { y: PercentInfo.toFixed(2), label: GetResourceLanguage("Info"), color: "#1997bb" },
                            { y: PercentWarning.toFixed(2), label: GetResourceLanguage("Warning"), color: "#bb9b19" },
                            { y: PercentError.toFixed(2), label: GetResourceLanguage("Error"), color: "#c14949" },
                            {
                                y: PercentException.toFixed(2),
                                label: GetResourceLanguage("Exception"),
                                color: "#ff0000"
                            }
                        ]
                    }
                ]
            });
        chartTotal.render();
        ChartsItems[0] = chartTotal;
    };

    // Chart Time Log
    var chartTime;
    ChartsCreatorFunc[1] = function CreateChartForTimeLog() {
        chartTime = new CanvasJS.Chart("ChartForTimeLog",
            {
                backgroundColor: "",
                zoomEnabled: true,
                animationEnabled: true,
                animationDuration: 639,
                title: {
                    text: GetResourceLanguage("HourlyLogStatistics"),
                    fontColor: "#eee"
                },
                axisX: {
                    interval: 2,
                    intervalType: "hour",
                    valueFormatString: "HH:mm",
                    labelFontColor: "#eee",
                    titleFontColor: "#eee"
                },
                axisY: {
                    includeZero: false,
                    title: GetResourceLanguage("Count"),
                    labelFontColor: "#eee",
                    titleFontColor: "#eee"
                },
                legend: {
                    cursor: "pointer",
                    itemclick: toggleLegendItems,
                    fontColor: "#eee"
                },
                toolTip: {
                    enabled: true,
                    animationEnabled: true,
                    shared: true,
                    contentFormatter: function(e) {
                        var str = "";
                        for (let i = 0; i < e.entries.length; i++) {
                            var currentTime = e.entries[i].dataPoint.x;
                            var temp = GetResourceLanguage("Hour") +
                                ": " +
                                (currentTime.getHours().toString().length === 1
                                    ? `0${currentTime.getHours()}`
                                    : currentTime.getHours()) +
                                ":" +
                                (currentTime.getMinutes().toString().length === 1
                                    ? `0${currentTime.getMinutes()}`
                                    : currentTime.getMinutes()) +
                                " - <a style='color: " +
                                e.entries[i].dataSeries.color +
                                "'>" +
                                GetResourceLanguage("NumberOf") +
                                " " +
                                e.entries[i].dataSeries.name +
                                ": " +
                                NumberFormatForShow(e.entries[i].dataPoint.y) +
                                "</a><br/>";
                            str = str.concat(temp);
                        }
                        return (str);
                    }
                },
                data: [
                    {
                        name: GetResourceLanguage("Event"),
                        type: "spline",
                        yValueFormatString: "#0.## Event",
                        color: "#49c162",
                        lineDashType: "dash",
                        dataPoints: eventStatisticsDataPoint
                    },
                    {
                        name: GetResourceLanguage("Info"),
                        type: "spline",
                        yValueFormatString: "#0.## Information",
                        color: "#1997bb",
                        lineDashType: "dash",
                        dataPoints: infoStatisticsDataPoint
                    },
                    {
                        name: GetResourceLanguage("Warning"),
                        type: "spline",
                        yValueFormatString: "#0.## Warning",
                        color: "#bb9b19",
                        lineDashType: "dash",
                        dataPoints: warnStatisticsDataPoint
                    },
                    {
                        name: GetResourceLanguage("Error"),
                        type: "spline",
                        yValueFormatString: "#0.## Error",
                        color: "#c14949",
                        lineDashType: "dash",
                        dataPoints: errorStatisticsDataPoint
                    },
                    {
                        name: GetResourceLanguage("Exception"),
                        type: "spline",
                        yValueFormatString: "#0.## Exception",
                        color: "#ff0000",
                        lineDashType: "dash",
                        dataPoints: excepStatisticsDataPoint
                    }
                ]
            });
        chartTime.render();
        ChartsItems[1] = chartTime;
    };

    // Chart Ave log time
    var chartAverage;
    ChartsCreatorFunc[2] = function CreateChartForAveLogTime() {
        chartAverage = new CanvasJS.Chart("ChartForAveLogTime",
            {
                backgroundColor: "",
                animationEnabled: true,
                animationDuration: 639,
                theme: "light2", // "light1", "light2", "dark1", "dark2"
                title: {
                    text: GetResourceLanguage("AverageNumberOfRecordedLogs") +
                        " " +
                        GetResourceLanguage("In") +
                        " " +
                        GetResourceLanguage("Time"),
                    fontColor: "#eee"
                },
                legend: {
                    cursor: "pointer",
                    itemclick: toggleLegendItems,
                    fontColor: "#eee"
                },
                axisY: {
                    includeZero: false,
                    title: GetResourceLanguage("Count"),
                    titleFontWeight: "bold",
                    labelFontColor: "#eee",
                    titleFontColor: "#eee"
                },
                axisX: {
                    labelFontColor: "#eee",
                    titleFontColor: "#eee"
                },
                toolTip: {
                    enabled: true,
                    animationEnabled: true,
                    shared: true,
                    contentFormatter: function(e) {
                        var str = "";
                        for (let i = 0; i < e.entries.length; i++) {
                            var temp =
                                `<b><a style='color: ${e.entries[i].dataPoint.color}'>${GetResourceLanguage("Count")} ${
                                    GetResourceLanguage("In")} ${e.entries[i].dataPoint.label}: 
                                    ${NumberFormatForShow(e.entries[i].dataPoint.y)} ${GetResourceLanguage("Log")
                                    }</a></b>`;
                            str = str.concat(temp);
                        }
                        return (str);
                    }
                },
                data: [
                    {
                        type: "column",
                        yValueFormatString: "#,##0.0#",
                        dataPoints: [
                            { label: GetResourceLanguage("Hour"), y: aveHour },
                            { label: GetResourceLanguage("Minute"), y: aveMinute },
                            { label: GetResourceLanguage("Second"), y: aveSecond }
                        ]
                    }
                ]
            });
        chartAverage.render();
        ChartsItems[2] = chartAverage;
    };

    // Enable and disable legend Items
    function toggleLegendItems(e) {
        if (e.dataSeries.type === "spline") {
            if (typeof (e.dataSeries.visible) === "undefined" || e.dataSeries.visible) {
                e.dataSeries.visible = false;
                e.dataSeries.legendMarkerType = "cross";
            } else {
                e.dataSeries.visible = true;
                e.dataSeries.legendMarkerType = "circle";
            }
            e.chart.render();
        } else if (e.dataSeries.type === "pie" || e.dataSeries.type === "column") {
            if (e.dataPoint.hasOwnProperty("actualYValue") && e.dataPoint.actualYValue !== null) {
                e.dataPoint.y = e.dataPoint.actualYValue;
                e.dataPoint.actualYValue = null;
                e.dataPoint.indexLabelFontSize = null;
                e.dataPoint.indexLabelLineThickness = null;
                e.dataPoint.legendMarkerType = "circle";
            } else {
                e.dataPoint.actualYValue = e.dataPoint.y;
                e.dataPoint.y = 0;
                e.dataPoint.indexLabelFontSize = 0;
                e.dataPoint.indexLabelLineThickness = 0;
                e.dataPoint.legendMarkerType = "cross";
            }
            e.chart.render();
        }
    }

    // Refresh 'render()' all chart
    function RefreshAllChart() {
        for (let i = 0; i < ChartsItems.length; i++) {
            ChartsItems[i].render();
        }
    }

    // Reset 'render()' all chart
    function ResetAllChart() {
        for (let i = 0; i < ChartsItems.length; i++) {
            ChartsItems[i].destroy();
        }

        // Calculate charts values
        CalculateChartsValues();

        for (let j = 0; j < ChartsCreatorFunc.length; j++) {
            ChartsCreatorFunc[j]();
        }
    }

    // Create all chart in first time
    for (let j = 0; j < ChartsCreatorFunc.length; j++) {
        ChartsCreatorFunc[j]();
    }

    var rtlLinkStyle = $('<link href="Utilities/CSS/G9LogReaderStyle-rtl.css" rel="stylesheet" />');

    // Listen for the event 'onChangeLanguage'
    $(window).on("onChangeLanguage", function(e) { SetRequirementByLanguage(e); });

    // Listen for the event 'onStarterLanguage'
    $(window).on("onStarterLanguage", function(e) { SetRequirementByLanguage(e); });

    function SetRequirementByLanguage(lang) {
        if (lang.detail.Direction === "ltr") {
            $(rtlLinkStyle).remove();
        } else {
            $("head").append(rtlLinkStyle);
        }

        if (indexOfDataLogFile === settingFileCount)
            ResetAllChart();
    }

    // ##################### Log Counter Handler #####################
    // Each all counter item
    function ShowLogCounter() {
        $(".LogInformationDiv_CountNumber").each(function(index) {
            AnimateLogCounter($(this));
        });
    }

    // Animate counter item from zero to value
    function AnimateLogCounter(selector) {
        var finishValue = parseInt($(selector).text().replace(",", ""));
        var counterPlus = finishValue / 199;
        var startValue = 0;
        var animateCounterInterval = setInterval(function() {
                $(selector).text(NumberFormatForShow(parseInt(startValue += counterPlus)));
                if (startValue >= finishValue) {
                    $(selector).text(NumberFormatForShow(parseInt(finishValue)));
                    clearInterval(animateCounterInterval);
                }
            },
            1);
    }

    // ##################### Logs Data #####################

    // Handler for show date time clock
    function ShowDateTimeClock() {
        var today = new Date();
        $("#ContentShowLog_Items_Header_DateTimeClock")
            .text(
                ConvertDateTimeWithCulture(
                    today.getFullYear() +
                    "/" +
                    PadLeft("00", today.getMonth() + 1) +
                    "/" +
                    PadLeft("00", today.getDate()) +
                    " " +
                    PadLeft("00", today.getHours()) +
                    ":" +
                    PadLeft("00", today.getMinutes()) +
                    ":" +
                    PadLeft("00", today.getSeconds())
                )
            );
        setTimeout(ShowDateTimeClock, 1000);
    }

    ShowDateTimeClock();

    var indexOfDataLogFile = 0;

    // Load all data log script
    function AddAllDataLogScript() {
        try {
            if (settingFileCount === 0) {
                $("#DivLogin_Loading").html(`{Log Data Not Found}<br />{Data Directory IS Empty}`);
                return;
            }
            var script = document.createElement("script");
            script.onload = function() {
                indexOfDataLogFile++;
                $("#DivLogin_Loading").html(`{LOADING}<br />{${indexOfDataLogFile}/${settingFileCount}}`);
                if (indexOfDataLogFile < settingFileCount)
                    AddAllDataLogScript();
                else {

                    // Handle add all log data
                    HandlerForAddAllLogData();

                    setTimeout(function() {
                            // Hide loading
                            $("#DivLogin_Loading").fadeOut(169,
                                function() {
                                    // Show login if encryptin active
                                    if (G9Encoding) {
                                        $("#DivLogin").css("display", "");
                                        $("#DivLogin").fadeOut(0);
                                        $("#DivLogin").fadeIn(169);
                                    } else {
                                        HideLoginAndLoadingPanel(function() {
                                            SwitchMenuByItemMenuNumber(G9DefaultPage);
                                        });
                                    }
                                });
                        },
                        639);
                }
            };
            script.src = `Data/${indexOfDataLogFile}-G9DataLog.js?v${Math.random()}`;
            document.getElementsByTagName("head")[0].appendChild(script);

        } catch (error) {
            console.log(error);
        }
    }

    // Handle add all log data
    function HandlerForAddAllLogData() {
        // Set min and max time
        var minHourAndMinetes = parseInt(G9DataLog[0][3].substring(11, 13)) * 60 +
            parseInt(G9DataLog[0][3].substring(14, 16));
        var maxHourAndMinetes = parseInt(G9DataLog[G9DataLog.length - 1][3].substring(11, 13)) * 60 +
            parseInt(G9DataLog[G9DataLog.length - 1][3].substring(14, 16));
        MinAndMaxHourAndMinutesTime = [minHourAndMinetes, maxHourAndMinetes];

        // Calculate log count
        for (let i = 0; i < G9DataLog.length; i++) {
            // Add programmatically ItemNumber to array
            G9DataLog[i][4] = i;

            // Get hour from time
            // DateTime Format: yyyy/MM/dd hh:mm:ss <=> 2019/08/16 01:12:12
            var currentHour = parseInt(G9DataLog[i][3].substring(11, 13));
            // Calculate count and set count in hour
            switch (G9DataLog[i][0]) {
            case 0:
                CountEvent++;
                StatisticsEvent[currentHour]++;
                break;
            case 1:
                CountInfo++;
                StatisticsInfo[currentHour]++;
                break;
            case 2:
                CountWarning++;
                StatisticsWarning[currentHour]++;
                break;
            case 3:
                CountError++;
                StatisticsError[currentHour]++;
                break;
            case 4:
                CountException++;
                StatisticsException[currentHour]++;
                break;
            default:
                console.log(`Log type '${G9DataLog[i][0]}' not supported!`);
                return;
            }
        }
    }

    // Set and Calculate charts values
    function CalculateChartsValues() {
        // Calculate Percent
        // Chart 0
        var total = CountEvent + CountInfo + CountWarning + CountError + CountException;
        PercentEvent = CountEvent / total * 100;
        PercentInfo = CountInfo / total * 100;
        PercentWarning = CountWarning / total * 100;
        PercentError = CountError / total * 100;
        PercentException = CountException / total * 100;
        // Calculate Statistics
        // Chart 1
        eventStatisticsDataPoint = [], infoStatisticsDataPoint = [], warnStatisticsDataPoint =
            [], errorStatisticsDataPoint = [], excepStatisticsDataPoint = [];
        var staticsDate = G9DataLog[0][3].substring(0, 11);
        for (let i = 0; i < 24; i++) {
            eventStatisticsDataPoint.push({
                x: new Date(staticsDate + PadLeft("00", i) + ":00:00"),
                y: StatisticsEvent[i]
            });
            infoStatisticsDataPoint.push({
                x: new Date(staticsDate + PadLeft("00", i) + ":00:00"),
                y: StatisticsInfo[i]
            });
            warnStatisticsDataPoint.push({
                x: new Date(staticsDate + PadLeft("00", i) + ":00:00"),
                y: StatisticsWarning[i]
            });
            errorStatisticsDataPoint.push({
                x: new Date(staticsDate + PadLeft("00", i) + ":00:00"),
                y: StatisticsError[i]
            });
            excepStatisticsDataPoint.push({
                x: new Date(staticsDate + PadLeft("00", i) + ":00:00"),
                y: StatisticsException[i]
            });
        }
        // Calculate Average
        // Chart 2
        aveHour = Math.ceil(total / 24);
        aveMinute = Math.ceil(aveHour / 60);
        aveSecond = Math.ceil(aveMinute / 60);
    }

    // Field save current index for log data
    var currentLogDataIndex = 0;

    // Field specify count of item for show
    var countOfLogDataForShow = 2;

    // Array for save filtered Data Log
    var FilteredDataLog = [];

    // If enable decrypted data before add to 'FilteredDataLog'
    // When seach filter set on Body, Path, Method or line => enable automatic this field
    // Because need decrypt before seach
    var EnableDecryptionBeforeShow = false;

    // Use for filter _hourFilter => Array(int StartRangeHHmm, int EndRangeHHmm)
    var _hourFilter = [0, 2359];
    // Use for filter _enableLogTypeArray => Array(bool enableEVENT, bool enableINFO, bool enableWARN, bool enableERROR, bool enableEXCEPTION)
    var _enableLogTypeArray = [true, true, true, true, true];
    // Use for filter _enableLogItemForSearch => Array(bool enableIdentity, bool enableTitle, bool enableBody, bool enablePath, bool enableMethod, bool enableLine)
    var _enableLogItemForSearch = [true, false, false, false, false, false];
    // Use for filter _textForSearchLogItems => string textForSearch
    var _textForSearchLogItems = "";
    // Interval for find item
    var _intervalForFindItem;

    // Function for set filter on G9DataLogs
    // Parameter hourFilter => Array(int StartRangeHHmm, int EndRangeHHmm)
    // Parameter enableLogTypeArray => Array(bool enableEVENT, bool enableINFO, bool enableWARN, bool enableERROR, bool enableEXCEPTION)
    // Parameter enableLogItemForSearch => Array(bool enableIdentity, bool enableTitle, bool enableBody, bool enablePath, bool enableMethod, bool enableLine)
    // Parameter textForSearchLogItems => string textForSearch
    function SetFilterOnG9DataLog(hourFilter, enableLogTypeArray, enableLogItemForSearch, textForSearchLogItems) {
        $("#ContentShowLog_Items_Body").empty();
        var resetStarterSlider = false;

        // ############## Save filters ##############
        // Set _hourFilter
        if (hourFilter) {
            _hourFilter = hourFilter;
            resetStarterSlider = true;
        }
        // Set _enableLogTypeArray
        if (enableLogTypeArray) _enableLogTypeArray = enableLogTypeArray;
        // Set search filter
        if (enableLogItemForSearch) _enableLogItemForSearch = enableLogItemForSearch;
        // Set text for search
        if (textForSearchLogItems || textForSearchLogItems === "") _textForSearchLogItems = textForSearchLogItems;
        // If Enable search for Title, Body, Path, Method or line => set true 'EnableDecryptionBeforeShow'
        if (_enableLogItemForSearch[1] ||
            _enableLogItemForSearch[2] ||
            _enableLogItemForSearch[3] ||
            _enableLogItemForSearch[4] ||
            _enableLogItemForSearch[5])
            EnableDecryptionBeforeShow = true;
        else
            EnableDecryptionBeforeShow = false;

        $("#ShowTextFilter_SeachEnter_ProgressBar").css("width", "0%");

        FilteredDataLog = [];
        var itemNumber = 0;

        clearInterval(_intervalForFindItem);

        // If Search text disable
        if (!EnableDecryptionBeforeShow || _textForSearchLogItems === null || _textForSearchLogItems.length <= 0) {
            for (itemNumber = 0; itemNumber < G9DataLog.length; itemNumber++) {
                if (_hourFilter) {
                    // DateTime Format: yyyy/MM/dd hh:mm:ss <=> 2019/08/16 01:12:12
                    var currentHour = parseInt(G9DataLog[itemNumber][3].substring(11, 13) +
                        G9DataLog[itemNumber][3].substring(14, 16));
                    // If item hour not in filter range continue
                    if (currentHour < _hourFilter[0] || currentHour > _hourFilter[1])
                        continue;
                }

                // If log type disable => continue
                if (!_enableLogTypeArray[G9DataLog[itemNumber][0]])
                    continue;

                // If search text is not empty
                if (_textForSearchLogItems && _textForSearchLogItems.length > 0) {
                    // if find item return true else return false and continue
                    if (!G9LogDataSeachHandler(_textForSearchLogItems, itemNumber))
                        continue;
                }

                // Add data to array 'FilteredDataLog' like pointer
                AddFilteredDataLog(itemNumber);
            }

            RemoveAndLoadDataLogsByIndexLogNumber(0, true, resetStarterSlider);
        } else {
            var findFirst = false;
            var itemNumberInterval = 0;
            var maxItemInteval = G9DataLog.length;

            if (_hourFilter) {
                for (var i = 0; i < G9DataLog.length; i++) {
                    // DateTime Format: yyyy/MM/dd hh:mm:ss <=> 2019/08/16 01:12:12
                    var eachHour = parseInt(G9DataLog[i][3].substring(11, 13) + G9DataLog[i][3].substring(14, 16));
                    if (eachHour >= _hourFilter[0]) {
                        var counter = i - 1;
                        itemNumberInterval = counter < 0 ? 0 : counter;
                        break;
                    }
                }
                for (var j = itemNumberInterval; j < G9DataLog.length; j++) {
                    // DateTime Format: yyyy/MM/dd hh:mm:ss <=> 2019/08/16 01:12:12
                    var eachHourLast = parseInt(G9DataLog[j][3].substring(11, 13) + G9DataLog[j][3].substring(14, 16));
                    if (eachHourLast > _hourFilter[1]) {
                        maxItemInteval = j > G9DataLog.length ? G9DataLog.length : j - 1;
                        break;
                    }
                }
            }

            var lenghtForSearch = maxItemInteval - itemNumberInterval;
            var darsad = lenghtForSearch / 100;

            _intervalForFindItem = setInterval(function() {

                    for (let fastSearch = 0; fastSearch <= 39; fastSearch++) {
                        let flagAddItem = true;

                        if (_hourFilter) {
                            // DateTime Format: yyyy/MM/dd hh:mm:ss <=> 2019/08/16 01:12:12
                            var currentHour = parseInt(G9DataLog[itemNumberInterval][3].substring(11, 13) +
                                G9DataLog[itemNumberInterval][3].substring(14, 16));
                            // If item hour not in filter range continue
                            if (currentHour < _hourFilter[0] || currentHour > _hourFilter[1])
                                flagAddItem = false;
                        }

                        // If log type disable => continue
                        if (!_enableLogTypeArray[G9DataLog[itemNumberInterval][0]])
                            flagAddItem = false;

                        // If search text is not empty
                        if (_textForSearchLogItems && _textForSearchLogItems.length > 0) {

                            // Decrypted before search if need
                            DecryptG9DataLogByItemNumber(itemNumberInterval);

                            // if find item return true else return false and continue
                            if (!G9LogDataSeachHandler(_textForSearchLogItems, itemNumberInterval))
                                flagAddItem = false;
                        }

                        // Add data if need
                        if (flagAddItem) {
                            AddFilteredDataLog(itemNumberInterval);
                            if (!findFirst) {
                                RemoveAndLoadDataLogsByIndexLogNumber(0, true, true);
                                findFirst = true;
                            } else {
                                AddNewDataLogsAndRefreshData(FilteredDataLog.length - 1, true, false);
                            }
                        }

                        $("#ShowTextFilter_SeachEnter_ProgressBar")
                            .css("width", (100 - ((maxItemInteval - itemNumberInterval) / darsad)) + "%");

                        if (++itemNumberInterval >= maxItemInteval) {
                            clearInterval(_intervalForFindItem);
                            return;
                        }
                    }
                },
                1);

        }

    }

    // Handler for seach
    // If find item return true else return false
    function G9LogDataSeachHandler(text, numberOfItem, mathText) {
        // Search Identity
        if (_enableLogItemForSearch[0] && StringEqualWithoutCaseSensitive(G9DataLog[numberOfItem][1], text, mathText))
            return true;
        // Search Title
        if (_enableLogItemForSearch[1] &&
            StringEqualWithoutCaseSensitive(G9DataLog[numberOfItem][2][0], text, mathText))
            return true;
        // Search body
        if (_enableLogItemForSearch[2] &&
            StringEqualWithoutCaseSensitive(G9DataLog[numberOfItem][2][1], text, mathText))
            return true;
        // Search path
        if (_enableLogItemForSearch[3] &&
            StringEqualWithoutCaseSensitive(G9DataLog[numberOfItem][2][2], text, mathText))
            return true;
        // Search method
        if (_enableLogItemForSearch[4] &&
            StringEqualWithoutCaseSensitive(G9DataLog[numberOfItem][2][3], text, mathText))
            return true;
        // Search line
        if (_enableLogItemForSearch[5] &&
            StringEqualWithoutCaseSensitive(G9DataLog[numberOfItem][2][4], text, mathText))
            return true;
        // if not find
        return false;
    }

    // Compare two text without case sensutuve
    function StringEqualWithoutCaseSensitive(a, b, mathText) {
        if (mathText)
            return a.indexOf(b) !== -1;
        else
            return a.search(new RegExp(b, "i")) !== -1;
    }

    // Add item to 'FilteredDataLog'
    function AddFilteredDataLog(itemNumber) {
        // Work like pointer - return refrence 'FilteredDataLog'
        FilteredDataLog.push(function() {
            var number = itemNumber;
            return G9DataLog[number];
        });
    }

    // Delete all log if exists and add new log data by starter index
    function RemoveAndLoadDataLogsByIndexLogNumber(logDataNumber, needRefreshMaxSlider, needRefreshValueSlider) {
        // logType, identity, title, body, fileName, methodBase, lineNumber, logDateTime, Decoded

        if (logDataNumber === parseInt(logDataNumber, 10)) {
            currentLogDataIndex = logDataNumber;
        }

        var forCounter = currentLogDataIndex + countOfLogDataForShow;
        if (forCounter > FilteredDataLog.length) {
            forCounter = FilteredDataLog.length;
        }

        $("#ContentShowLog_Items_Body").empty();
        for (let i = currentLogDataIndex; i < forCounter; i++) {
            AddItemLogByItemNumber(i, false);
        }
        SetCustomScroll(FilteredDataLog.length, needRefreshMaxSlider, needRefreshValueSlider);
    }

    function AddNewDataLogsAndRefreshData(logNumber, needRefreshMaxSlider, needRefreshValueSlider) {

        if ($(".G9LogBody").length <= countOfLogDataForShow) {
            AddItemLogByItemNumber(logNumber, false);
        }
        SetCustomScroll(FilteredDataLog.length, needRefreshMaxSlider, needRefreshValueSlider);
    }

    // Add log data for show by itemNumber
    // params:
    // itemNumber => Specify item number
    // addAtFirst => if true add as first item else add last item
    function AddItemLogByItemNumber(itemNumber, addAtFirst) {
        // If item not exists return
        if (itemNumber >= FilteredDataLog.length) {
            return;
        }
        // Decrypt item
        DecryptFilteredDataLogByItemNumber(itemNumber);

        // Get main item
        var mainItem = FilteredDataLog[itemNumber]();

        var logIdentity = `LogIdentity_${mainItem[4]}`;
        var tooltipIdentity = `TooltipIdentity_${mainItem[4]}`;
        var baseDataLog =
            `
                    <div id="${logIdentity}" ItemNumber="${itemNumber}" class="container-fluid G9LogBody ${
                GetClassNameByLogType(mainItem[0])}">
                        <div class="G9LogBody_Selector"></div>
                        <div class="G9LogBody_LogNumber">
                            <input type="text" value="${mainItem[4]}" readonly />
                            <div class="G9LogBody_BgItems_Copy ${tooltipIdentity
                }" data-toggle="tooltip" data-placement="right" title="${GetResourceLanguage("Copy")
                }" style="right: -6px; top: 0px; height: 27px;">
                                <img src="Utilities/ICON/DashboardIcon/copy-content.png" style="top: -3px;" />
                            </div>
                        </div>
                        <img class="G9LogBody_Icon" src="Utilities/ICON/LogTypeMode/${GetIconByLogType(mainItem[0])
                }.png">

                        <div class="row G9LogBody_Row">
                            <div class="col-4">
                                <div class="G9LogBody_BgItems">
                                    <div class="G9LogBody_BgItems_Title">
                                        <div>
                                            <img class="G9LogBody_BgItems_Title_Icon" src="Utilities/ICON/LogItemsIcon/0-Identity.png">
                                            <span class="G9LogTitles" g9-bind-value="Identity" >${GetResourceLanguage(
                    "Identity")}</span>
                                        </div>
                                    </div>
                                    <div class="G9LogBody_BgItems_Input">
                                        <input type="text" value="${mainItem[1]}" readonly />
                                        <div class="G9LogBody_BgItems_Copy ${tooltipIdentity
                }" data-toggle="tooltip" data-placement="top" title="${GetResourceLanguage("Copy")}">
                                        <img src="Utilities/ICON/DashboardIcon/copy-content.png" /></div>
                                    </div>
                                </div>
                            </div>
                            <div class="col-4">
                                <div class="G9LogBody_BgItems">
                                    <div class="G9LogBody_BgItems_Title">
                                        <div>
                                            <img class="G9LogBody_BgItems_Title_Icon" src="Utilities/ICON/LogItemsIcon/1-Title.png">
                                            <span class="G9LogTitles" g9-bind-value="Title" >${GetResourceLanguage(
                    "Title")}</span>
                                        </div>
                                    </div>
                                    <div class="G9LogBody_BgItems_Input"><input type="text" value="${mainItem[2][0]
                }" readonly />
                                    <div class="G9LogBody_BgItems_Copy ${tooltipIdentity
                }" data-toggle="tooltip" data-placement="top" title="${GetResourceLanguage("Copy")}">
                                    <img src="Utilities/ICON/DashboardIcon/copy-content.png" /></div></div>
                                </div>
                            </div>
                            <div class="col-4">
                                <div class="G9LogBody_BgItems">
                                    <div class="G9LogBody_BgItems_Title">
                                        <div>
                                            <img class="G9LogBody_BgItems_Title_Icon" src="Utilities/ICON/LogItemsIcon/2-DateTime.png">
                                            <span class="G9LogTitles" g9-bind-value="DateTime" >${GetResourceLanguage(
                    "DateTime")}</span>
                                        </div>
                                    </div>
                                    <div class="G9LogBody_BgItems_Input"><input type="text" g9-culture-datetime="${
                mainItem[3]}" value="${ConvertDateTimeWithCulture(mainItem[3])}" readonly />
                                    <div class="G9LogBody_BgItems_Copy ${tooltipIdentity
                }" data-toggle="tooltip" data-placement="top" title="${GetResourceLanguage("Copy")}">
                                    <img src="Utilities/ICON/DashboardIcon/copy-content.png" /></div></div>
                                </div>
                            </div>
                        </div>
                        <div class="row G9LogBody_Row">
                            <div class="col-12">
                                <div style="height: 86px;" class="G9LogBody_BgItems G9LogBody_BgItems_TextAreaDiv">
                                    <div class="G9LogBody_BgItems_Title">
                                        <div>
                                            <img class="G9LogBody_BgItems_Title_Icon" src="Utilities/ICON/LogItemsIcon/3-Body.png">
                                            <span class="G9LogTitles" g9-bind-value="Body" >${GetResourceLanguage(
                    "Body")}</span>
                                        </div>
                                    </div>
                                    <div class="G9LogBody_BgItems_Input"><textarea class="G9Body_TextArea" readonly>${
                mainItem[2][1]}</textarea>
                                    <div class="G9LogBody_BgItems_Copy G9LogBody_BgItems_Copy_TextArea ${
                tooltipIdentity}" data-toggle="tooltip" data-placement="top" title="${GetResourceLanguage("Copy")}">
                                    <img src="Utilities/ICON/DashboardIcon/copy-content.png" /></div>
                                    <div class="G9Body_TextArea_FrontCover"></div>
                                    </div>
                                </div>  
                            </div>
                        </div>
                        <div class="row G9LogBody_Row">
                            <div class="col-6">
                                <div class="G9LogBody_BgItems">
                                    <div class="G9LogBody_BgItems_Title">
                                        <div>
                                            <img class="G9LogBody_BgItems_Title_Icon" src="Utilities/ICON/LogItemsIcon/4-Path.png">
                                            <span class="G9LogTitles" g9-bind-value="Path" >${GetResourceLanguage(
                    "Path")}</span>
                                        </div>
                                    </div>
                                    <div class="G9LogBody_BgItems_Input"><input type="text" value="${mainItem[2][2]
                }" readonly />
                                    <div class="G9LogBody_BgItems_Copy ${tooltipIdentity
                }" data-toggle="tooltip" data-placement="top" title="${GetResourceLanguage("Copy")}">
                                    <img src="Utilities/ICON/DashboardIcon/copy-content.png" /></div></div>
                                </div>
                            </div>
                            <div class="col-4">
                                <div class="G9LogBody_BgItems">
                                    <div class="G9LogBody_BgItems_Title">
                                        <div>
                                            <img class="G9LogBody_BgItems_Title_Icon" src="Utilities/ICON/LogItemsIcon/5-Method.png">
                                            <span class="G9LogTitles" g9-bind-value="Method" >${GetResourceLanguage(
                    "Method")}</span>
                                        </div>
                                    </div>
                                    <div class="G9LogBody_BgItems_Input"><input type="text" value="${mainItem[2][3]
                }" readonly />
                                    <div class="G9LogBody_BgItems_Copy ${tooltipIdentity
                }" data-toggle="tooltip" data-placement="top" title="${GetResourceLanguage("Copy")}">
                                    <img src="Utilities/ICON/DashboardIcon/copy-content.png" /></div></div>
                                </div>
                            </div>
                            <div class="col-2">
                                <div class="G9LogBody_BgItems">
                                    <div class="G9LogBody_BgItems_Title">
                                        <div>
                                            <img class="G9LogBody_BgItems_Title_Icon" src="Utilities/ICON/LogItemsIcon/6-Line.png">
                                            <span class="G9LogTitles" g9-bind-value="Line" >${GetResourceLanguage(
                    "Line")}</span>
                                        </div>
                                    </div>
                                    <div class="G9LogBody_BgItems_Input"><input type="text" value="${mainItem[2][4]
                }" readonly />
                                    <div class="G9LogBody_BgItems_Copy ${tooltipIdentity
                }" data-toggle="tooltip" data-placement="top" title="${GetResourceLanguage("Copy")}">
                                    <img src="Utilities/ICON/DashboardIcon/copy-content.png" /></div></div>
                                </div>
                            </div>
                        </div>
                        <div class="G9LogBody_BgItems_LogType_MainDiv">
                            <div class="G9LogBody_BgItems">
                                <div class="G9LogBody_BgItems_Input">
                                    <div class="G9LogBody_BgItems_LogType ${GetClassNameByLogType(mainItem[0])
                }_Mode" style="text-align: center;">${GetErrorTypeName(mainItem[0])}</div>
                                </div>
                            </div>
                        </div>
                        <div class="G9LogBody_DarkCove"></div>
                    </div>
                `;

        if (addAtFirst) {
            $("#ContentShowLog_Items_Body").prepend(baseDataLog);
        } else {
            $("#ContentShowLog_Items_Body").append(baseDataLog);
        }

        $(`.${tooltipIdentity}`).tooltip("enable");

        CalculateAndSetCountOfLogDataForShow();

    }

    // On change resize - calculate and set data logs items
    $(window).resize(function() {
        // If page is data logs
        if (_specifyItemMenuActive === 1)
            CalculateAndSetCountOfLogDataForShow();
    });

    // Set 'countOfLogDataForShow' by Calculate heights
    function CalculateAndSetCountOfLogDataForShow() {
        var item = $(".G9LogBody").first();
        var itemMarginHeight = parseInt($(item).css("margin-top"), 10) + parseInt($(item).css("margin-bottom"), 10);
        var itemPaddingHeight =
            parseInt($(item).css("padding-top"), 10) + parseInt($(item).css("padding-bottom"), 10);
        var finialHeightSizePerItem = $(".G9LogBody").height() + itemMarginHeight + itemPaddingHeight;

        var itemCount = Math.ceil($("#ContentShowLog_Items_Body").height() / finialHeightSizePerItem);
        if (itemCount !== countOfLogDataForShow) {
            countOfLogDataForShow = itemCount;
            RemoveAndLoadDataLogsByIndexLogNumber();
        }
    }

    // Decrypt log data in 'FilteredDataLog' by log item number = (array index) 
    function DecryptFilteredDataLogByItemNumber(itemNumber) {
        // Get bin item number
        var binItemNumber = FilteredDataLog[itemNumber]()[4];
        // If encryption is enable and this data not encrypted
        if (G9Encoding && typeof G9DataLogDecoded[binItemNumber] === "undefined") {
            // Column 2 is data log and just this column encrypted
            FilteredDataLog[itemNumber]()[2] = Decoding(FilteredDataLog[itemNumber]()[2], G9UserName, G9Password);
            // Split column 2 to 5 Array columns:
            // ({title}🅖➒{body}🅖➒{fileName}🅖➒{methodBase}🅖➒{lineNumber})
            FilteredDataLog[itemNumber]()[2] = FilteredDataLog[itemNumber]()[2].split("\ud83c\udd56\u2792");
            // Save decoded number
            G9DataLogDecoded[binItemNumber] = true;
        }
        else if (typeof G9DataLogDecoded[binItemNumber] === "undefined") {
            // Split column 2 to 5 Array columns:
            // ({title}🅖➒{body}🅖➒{fileName}🅖➒{methodBase}🅖➒{lineNumber})
            FilteredDataLog[itemNumber]()[2] = FilteredDataLog[itemNumber]()[2].split("\ud83c\udd56\u2792");
            // Save decoded number
            G9DataLogDecoded[binItemNumber] = true;
        }
    }

    // Decrypt log data in 'G9DataLog' by log item number = (array index)
    function DecryptG9DataLogByItemNumber(itemNumber) {
        // If encryption is enable and this data not encrypted
        if (G9Encoding && typeof G9DataLogDecoded[itemNumber] === "undefined") {
            // Column 2 is data log and just this column encrypted
            G9DataLog[itemNumber][2] = Decoding(G9DataLog[itemNumber][2], G9UserName, G9Password);
            // Split column 2 to 5 Array columns:
            // ({title}🅖➒{body}🅖➒{fileName}🅖➒{methodBase}🅖➒{lineNumber})
            G9DataLog[itemNumber][2] = G9DataLog[itemNumber][2].split("\ud83c\udd56\u2792");
            // Save decoded number
            G9DataLogDecoded[itemNumber] = true;
        }
    }

    // Function specify icon name with log type
    function GetIconByLogType(logType) {
        switch (logType) {
        case 0:
            return "Event";
        case 1:
            return "Info";
        case 2:
            return "Warn";
        case 3:
            return "Error";
        case 4:
            return "Exception";
        default:
            console.log(`Log type '${logType}' not supported!`);
        }
    }

    // Function specify class name with log type
    function GetClassNameByLogType(logType) {
        switch (logType) {
        case 0:
            return "EVENT";
        case 1:
            return "INFO";
        case 2:
            return "WARN";
        case 3:
            return "ERROR";
        case 4:
            return "EXCEPTION";
        default:
            console.log(`Log type '${logType}' not supported!`);
        }
    }

    // Function specify class name with log type
    function GetErrorTypeName(logType) {
        switch (logType) {
        case 0:
            return "EVENT";
        case 1:
            return "INFO";
        case 2:
            return "WARN";
        case 3:
            return "ERROR";
        case 4:
            return "EXCEP";
        default:
            console.log(`Log type '${logType}' not supported!`);
        }
    }

    // Handler for copy button
    $(document).on("click",
        ".G9LogBody_BgItems_Copy",
        function() {
            // Select all text from input or text area
            $(this).prev().select();
            // Copy the text inside the text field
            document.execCommand("copy");
        });

    $("#ContentShowLog_Items_Loading").fadeOut();
    // Specify scroll is initialize or no
    var initializeScroll = false;
    // Field save max item for slider scroll
    var maxItemForSlider;

    // Set custom scroll for logs data
    function SetCustomScroll(max, needRefreshMax, needRefreshValue) {

        // Set max item count
        maxItemForSlider = max - 1;

        if (!initializeScroll) {
            // Set initialize
            initializeScroll = true;

            // ############################### Slider log data counts ###############################
            $("#ContentShowLog_CustomScrollInner").slider({
                orientation: "vertical",
                animate: "fast",
                range: "min",
                min: 0,
                max: maxItemForSlider,
                step: 1,
                value: 0
            });

            $("#ContentShowLog_CustomScrollInner").on("slidestart",
                function(event, ui) {
                    $("#ContentShowLog_CustomScrollInner").find(".ui-state-default div div").stop();
                    $("#ContentShowLog_CustomScrollInner").find(".ui-state-default div div")
                        .css("display", "inline-flex");
                    $("#ContentShowLog_CustomScrollInner").find(".ui-state-default div div").fadeIn(269);
                    ShowLoadingItems();
                });

            $("#ContentShowLog_CustomScrollInner").on("slide",
                function(event, ui) {
                    $("#ContentShowLog_CustomScrollInner")
                        .find(".ui-state-default div div")
                        .text(GetResourceLanguage("LogNumber") +
                            ": " +
                            NumberFormatForShow(maxItemForSlider - ui.value));
                    $("#ContentShowLog_Items_Header_ItemCount")
                        .text(
                            NumberFormatForShow(maxItemForSlider - ui.value) +
                            "/" +
                            NumberFormatForShow(maxItemForSlider)
                        );
                });

            $("#ContentShowLog_CustomScrollInner").on("slidestop",
                function(event, ui) {
                    setTimeout(function() {
                            $("#ContentShowLog_CustomScrollInner").find(".ui-state-default div div").stop();
                            $("#ContentShowLog_CustomScrollInner").find(".ui-state-default div div").fadeOut(269);
                            HideLoadingItems();
                        },
                        369);

                    setTimeout(function() {
                            RemoveAndLoadDataLogsByIndexLogNumber(parseInt(ui.value));
                        },
                        169);

                });

            $("#ContentShowLog_CustomScrollInner").on("slidechange",
                function(event, ui) {
                    $("#ContentShowLog_CustomScrollInner").find(".ui-state-default div div")
                        .text(GetResourceLanguage("LogNumber") + ": " + NumberFormatForShow(ui.value));
                    $("#ContentShowLog_Items_Header_ItemCount")
                        .text(
                            NumberFormatForShow(ui.value) + "/" + NumberFormatForShow(maxItemForSlider)
                        );
                });

            // Set sliders div
            $("#ContentShowLog_CustomScrollInner").find(".ui-state-default")
                .append(`<div><div style="display: none">${GetResourceLanguage("LogNumber")}: 0</div></div>`);

            // Show item number to item count
            $("#ContentShowLog_Items_Header_ItemCount").text(`0/${NumberFormatForShow(maxItemForSlider)}`);

            // ############################### Slider log data Date Time ###############################

            var minDateTimeSlider = MinAndMaxHourAndMinutesTime[1] === 2400
                ? MinAndMaxHourAndMinutesTime[0] + 1
                : MinAndMaxHourAndMinutesTime[0];

            var maxDateTimeSlider = MinAndMaxHourAndMinutesTime[1];

            $("#ContentShowLog_CustomScrollDateInner").slider({
                orientation: "vertical",
                animate: "fast",
                range: true,
                min: minDateTimeSlider,
                max: maxDateTimeSlider,
                step: 1,
                values: MinAndMaxHourAndMinutesTime
            });

            $("#ContentShowLog_CustomScrollDateInner").on("slidestart",
                function(event, ui) {
                    $("#ContentShowLog_CustomScrollDateInner").find(".ui-state-default div div").stop();
                    $("#ContentShowLog_CustomScrollDateInner").find(".ui-state-default div div")
                        .css("display", "inline-flex");
                    $("#ContentShowLog_CustomScrollDateInner").find(".ui-state-default div div").fadeIn(269);
                    ShowLoadingItems();
                    $(".ActiveFiltersDiv[type='filteTime']").fadeIn(369);
                });

            $("#ContentShowLog_CustomScrollDateInner").on("slide",
                function(event, ui) {
                    $("#ContentShowLog_CustomScrollDateInner")
                        .find(".ui-state-default div div")
                        .text(ConvertMinutesToHourAndMinutes(minDateTimeSlider + (maxDateTimeSlider - ui.values[1])) +
                            " - " +
                            ConvertMinutesToHourAndMinutes(maxDateTimeSlider - (ui.values[0] - minDateTimeSlider)));

                    $("#filteTime_Start")
                        .text(ConvertMinutesToHourAndMinutes(minDateTimeSlider + (maxDateTimeSlider - ui.values[1])));
                    $("#filteTime_End")
                        .text(ConvertMinutesToHourAndMinutes(maxDateTimeSlider - (ui.values[0] - minDateTimeSlider)));

                });

            $("#ContentShowLog_CustomScrollDateInner").on("slidestop",
                function(event, ui) {

                    setTimeout(function() {
                            $("#ContentShowLog_CustomScrollDateInner").find(".ui-state-default div div").stop();
                            $("#ContentShowLog_CustomScrollDateInner").find(".ui-state-default div div").fadeOut(269);
                            HideLoadingItems();
                        },
                        369);

                    setTimeout(function() {
                            // Set filter
                            SetFilterOnG9DataLog([
                                ConvertMinutesToHourAndMinutes(minDateTimeSlider + (maxDateTimeSlider - ui.values[1]),
                                    true),
                                ConvertMinutesToHourAndMinutes(maxDateTimeSlider - (ui.values[0] - minDateTimeSlider),
                                    true)
                            ]);
                            // Check filter
                            if (ui.values[0] ===
                                (MinAndMaxHourAndMinutesTime[1] === 2400
                                    ? MinAndMaxHourAndMinutesTime[0] + 1
                                    : MinAndMaxHourAndMinutesTime[0]) &&
                                ui.values[1] === MinAndMaxHourAndMinutesTime[1]) {
                                $(".ActiveFiltersDiv[type='filteTime']").fadeOut(369);
                            }
                        },
                        169);
                });

            $("#ContentShowLog_CustomScrollDateInner").on("slidechange",
                function(event, ui) {
                    $("#ContentShowLog_CustomScrollDateInner")
                        .find(".ui-state-default div div")
                        .text(ConvertMinutesToHourAndMinutes(minDateTimeSlider + (maxDateTimeSlider - ui.values[1])) +
                            " - " +
                            ConvertMinutesToHourAndMinutes(maxDateTimeSlider - (ui.values[0] - minDateTimeSlider)));

                    $("#filteTime_Start")
                        .text(ConvertMinutesToHourAndMinutes(minDateTimeSlider + (maxDateTimeSlider - ui.values[1])));
                    $("#filteTime_End")
                        .text(ConvertMinutesToHourAndMinutes(maxDateTimeSlider - (ui.values[0] - minDateTimeSlider)));
                });

            // Set sliders div
            $("#ContentShowLog_CustomScrollDateInner").find(".ui-state-default")
                .append(
                    `<div><div style="right: auto; left: 119%;display: none" >${
                    ConvertMinutesToHourAndMinutes(MinAndMaxHourAndMinutesTime[0])} - ${ConvertMinutesToHourAndMinutes(
                        MinAndMaxHourAndMinutesTime[1])}</div></div>`);
        }
        if (needRefreshMax) {
            // Remake slider
            $("#ContentShowLog_CustomScrollInner").slider("option", "max", maxItemForSlider);
            // Get Current value
            var valueOfSlider = $("#ContentShowLog_CustomScrollInner").slider("option", "value");
            // Show item number to item count
            $("#ContentShowLog_Items_Header_ItemCount").text(NumberFormatForShow(valueOfSlider) +
                "/" +
                NumberFormatForShow(maxItemForSlider));
        }
        if (needRefreshValue) {
            $("#ContentShowLog_CustomScrollInner").slider("value", 0);
        }
    }

    // Convert total minuets to hour:minutes
    // Parameter minutes => Total minutes
    // Parameter modeInt => parseInt(strint hour + string minutes)
    function ConvertMinutesToHourAndMinutes(minutes, modeInt) {
        if (minutes < 60) {
            if (modeInt)
                return parseInt(`00${PadLeft("00", minutes)}`);
            else
                return `00:${PadLeft("00", minutes)}`;
        } else {
            var leftOverIsHour = Math.floor(minutes / 60);
            var leftOverMinutes = minutes - (leftOverIsHour * 60);
            if (modeInt)
                return parseInt(PadLeft("00", leftOverIsHour) + PadLeft("00", leftOverMinutes));
            else
                return PadLeft("00", leftOverIsHour) + ":" + PadLeft("00", leftOverMinutes);
        }
    }

    // Show loading on grid items
    function ShowLoadingItems() {
        $("#ContentShowLog_Items_Body").css("filter", "blur(6.9px)");
        $("#ContentShowLog_Items_Loading").fadeIn(399);
    }

    // Hide loading on grid items
    function HideLoadingItems() {
        $("#ContentShowLog_Items_Body").css("filter", "");
        $("#ContentShowLog_Items_Loading").fadeOut(399);
    }

    // Specify mouse in text area ir no
    var _mouseEnterOnTextArea = false;
    // Specify time out after click
    var _clickDelayTimeOut = 369;
    // Specify lef out time out
    var _leftOutTimeOut = 0;

    // Handler for change size text area click
    $(document).on("click",
        ".G9Body_TextArea_FrontCover",
        function(e) {
            // If click on copy
            if ($(e.target).hasClass("G9LogBody_BgItems_Copy") ||
                $(e.target).parent().hasClass("G9LogBody_BgItems_Copy"))
                return;

            $(".G9Body_TextArea_FrontCover").css("display", "none");

            var mainSelector = $(this).parent().parent();

            // Set time out after click
            _leftOutTimeOut = _clickDelayTimeOut;
            var intervalForTimeOut = setInterval(function() {
                    _leftOutTimeOut--;
                    if (_leftOutTimeOut <= 0) {
                        clearInterval(intervalForTimeOut);
                    }
                },
                1);

            _mouseEnterOnTextArea = true;
            $(mainSelector).attr("FullScreen", "true");
            var previousParent = $(mainSelector).parent().parent().prev();
            var nextParent = $(mainSelector).parent().parent().next();
            $(previousParent).stop();
            $(nextParent).stop();
            $(mainSelector).stop();
            $(previousParent).fadeOut(0);
            $(nextParent).fadeOut(0);
            $(mainSelector).animate({ "height": "159px" }, 199);
            $(".G9LogBody_DarkCove").not($(mainSelector).parent().parent().parent().find(".G9LogBody_DarkCove"))
                .fadeIn(199);
            $(".G9LogBody").not($(mainSelector).parent().parent().parent())
                .css({ "filter": "blur(3.9px)", "-webkit-filter": "blur(3.9px)" }, 199);

        });

    // Interval controller for arrows
    var _timeOutScrollItemWithArrow;

    // Default starter time for interval
    var _timeOutScrollItemWithArrowDefaultTime = 169;

    // Handle arrow up
    $(document).on("mousedown",
        "#ContentShowLog_Items_Header",
        function() {
            HandleScrollDataLog(1);
            TimeOutHandlerForScroll(function() {
                HandleScrollDataLog(1);
            });
        });

    // Handle arrow down
    $(document).on("mousedown",
        "#ContentShowLog_Items_Footer",
        function() {
            HandleScrollDataLog(-1);
            TimeOutHandlerForScroll(function() {
                HandleScrollDataLog(-1);
            });
        });

    // Handler mouse up for down and up arrow
    $(document).on("mouseup",
        "#ContentShowLog_Items_Header, #ContentShowLog_Items_Footer",
        function() {
            clearInterval(_timeOutScrollItemWithArrow);
            _timeOutScrollItemWithArrowDefaultTime = 169;
        });

    // Handle time out for scroll item
    function TimeOutHandlerForScroll(funcAction) {
        _timeOutScrollItemWithArrow = setTimeout(function() {
                funcAction();
                _timeOutScrollItemWithArrowDefaultTime = _timeOutScrollItemWithArrowDefaultTime / 1.3;
                if (_timeOutScrollItemWithArrowDefaultTime <= 9)
                    _timeOutScrollItemWithArrowDefaultTime = 9;
                TimeOutHandlerForScroll(funcAction);
            },
            _timeOutScrollItemWithArrowDefaultTime);
    }

    // Timeout for exit fullscreen
    var _timeOutForHide;

    // Handler for mouse on text area div
    $(document).on({
            mouseenter: function() {
                // If click on copy
                clearTimeout(_timeOutForHide);
            },
            mouseleave: function() {
                TextAreaDivExitFullScreenHandler($(this));
            }
        },
        ".G9LogBody_BgItems_TextAreaDiv");

    function TextAreaDivExitFullScreenHandler(mainSelector, force) {
        if ($(mainSelector).attr("FullScreen")) {
            clearTimeout(_timeOutForHide);
            let timeoutTime = 9;
            if (force)
                timeoutTime = 1;
            _timeOutForHide = setTimeout(function() {
                    $(mainSelector).removeAttr("FullScreen");
                    $(".G9Body_TextArea_FrontCover").css("display", "");
                    _mouseEnterOnTextArea = false;
                    var previousParent = $(mainSelector).parent().parent().prev();
                    var nextParent = $(mainSelector).parent().parent().next();
                    $(previousParent).stop();
                    $(nextParent).stop();
                    $(mainSelector).stop();
                    $(mainSelector).animate({ "height": "86px" }, 199);
                    $(previousParent).fadeIn(0);
                    $(nextParent).fadeIn(0);
                    $(".G9LogBody_DarkCove").fadeOut(199);
                    $(".G9LogBody").css({ "filter": "blur(0px)", "-webkit-filter": "blur(0px)" }, 199);
                },
                timeoutTime + _leftOutTimeOut);
        }
    }

    _mouseEnterOnNavBar = false;
    // Handler for mouse on nav bar
    $(document).on({
            mouseenter: function() {
                _mouseEnterOnNavBar = true;
            },
            mouseleave: function() {
                _mouseEnterOnNavBar = false;
            }
        },
        ".sidebar-content");

    var _timeOutForMouseWheel = 30;

    // Handler mouse wheel for scroll logs data
    $(window).bind("DOMMouseScroll mousewheel",
        function(e) {
            var delta = e.originalEvent.wheelDelta ? e.originalEvent.wheelDelta : -e.detail;

            // Disable mouse wheel if
            // mouse on text area
            // mouse on nav bar meni
            // page is not log data
            if (_mouseEnterOnTextArea || _mouseEnterOnNavBar || _specifyItemMenuActive !== 1)
                return;

            setTimeout(function() {
                    _timeOutForMouseWheel -= 30;
                    // Scroll data
                    HandleScrollDataLog(delta);
                },
                _timeOutForMouseWheel);
            _timeOutForMouseWheel += 30;

        });

    // Handle scroll data log with delta
    // If delta > 0 scroll up else scroll down
    function HandleScrollDataLog(delta) {
        var item, itemNumber, newIndex;
        if (delta > 0) {
            // If item not exists return
            if (currentLogDataIndex - 1 < 0) {
                return;
            }
            item = $("#ContentShowLog_Items_Body .G9LogBody:last");
            itemNumber = parseInt($(item).attr("ItemNumber"));
            // Remove last item if > countOfLogDataForShow
            if (FilteredDataLog.length - itemNumber >= countOfLogDataForShow) {
                // Remove Item
                $(item).remove();
                // Set current index
                currentLogDataIndex = newIndex = itemNumber - countOfLogDataForShow;
            } else {
                // Set current index
                itemFirst = $("#ContentShowLog_Items_Body .G9LogBody:first");
                itemNumberFirst = parseInt($(itemFirst).attr("ItemNumber"));

                if (FilteredDataLog.length - itemNumberFirst > countOfLogDataForShow) {
                    // Remove Item
                    $(item).remove();
                }
                currentLogDataIndex = newIndex = itemNumberFirst - 1;
            }
            // If new index not exists in data log array return
            if (newIndex < 0) {
                return;
            }
            // Add new data log
            AddItemLogByItemNumber(newIndex, true);
            // Set slider
            $("#ContentShowLog_CustomScrollInner").slider("value", currentLogDataIndex);
            // Remove all infos
            $(".tooltip").remove();
        } else {
            // If item not exists return
            if (currentLogDataIndex + 1 >= FilteredDataLog.length) {
                return;
            }
            item = $("#ContentShowLog_Items_Body .G9LogBody:first");
            itemNumber = parseInt($(item).attr("ItemNumber"));
            // If item not exists return
            if (itemNumber + 1 >= FilteredDataLog.length) {
                return;
            } else {
                // Set current index
                currentLogDataIndex = itemNumber + 1;
            }
            $(item).remove();
            // Set slider
            $("#ContentShowLog_CustomScrollInner").slider("value", currentLogDataIndex);
            // Remove all infos
            $(".tooltip").remove();
            // If new index not exists in data log array return
            newIndex = itemNumber + countOfLogDataForShow;
            if (newIndex >= FilteredDataLog.length) {
                return;
            }
            // Add new data log
            AddItemLogByItemNumber(newIndex, false);

        }
    }

    // ################################# Login ######################################
    // Fade error message first time
    $("#DivLogin_Body_Message").fadeOut(0);
    // Handler for button login
    $("#Btn_Login").click(function() {
        var user = $("#G9UserName").val();
        var pass = $("#G9Password").val();
        if (user && pass) {
            user = user.length === 16
                ? user
                : user.length > 16
                ? user.substring(0, 16)
                : PadRight("9999999999999999", user);

            pass = pass.length === 16
                ? pass
                : pass.length > 16
                ? pass.substring(0, 16)
                : PadRight("9999999999999999", pass);

            let tempAllKeys = user + pass;
            tempAllKeys = md5(tempAllKeys);
            LogUserName = tempAllKeys.substring(0, 16);
            LogPassword = tempAllKeys.substring(16, 32);
            var encodingData = Decoding(G9Encoding, LogUserName, LogPassword);
            var DefaultEncodingSampleText = "This Is G9\u2122 Team!";
            if (encodingData && StringEqualWithoutCaseSensitive(encodingData, DefaultEncodingSampleText)) {
                // Set Global user pass
                G9UserName = LogUserName;
                G9Password = LogPassword;
                // Show page
                HideLoginAndLoadingPanel(function() {
                    SwitchMenuByItemMenuNumber(G9DefaultPage);
                });
            } else {
                ShowErrorMessageInLogin("ErrorIncorrectUserPass");
            }
        } else {
            ShowErrorMessageInLogin("ErrorEnterUserPass");
        }
    });

    // Set default State
    $("#UserLoginInfo").fadeOut(0);
    $(".sidebar-search").fadeOut(0);

    // Specify initialize
    var _isInitializeDashboardPanel = false;

    // Handler for hide login panel and show dashboard panel
    function HandleHideLoginPanelAndShowDashboardPanel(afterLogin) {
        $(".sidebar-search").fadeOut(369,
            function() {
                $("#UserLoginInfo").fadeIn(369);
            });

        if (afterLogin) {
            $("#ContentShowLog").fadeOut(369);
            setTimeout(function() {
                    $("#PanelInformation, #ContentForShowLogFile").fadeIn(369);
                },
                369);
        } else {
            $("#PanelInformation, #ContentForShowLogFile").fadeIn(369);
        }

        if (!_isInitializeDashboardPanel) {
            _isInitializeDashboardPanel = true;
            ResetAllChart();
        }

        setTimeout(function() {
                // Set item menu number
                _specifyItemMenuActive = 0;
            },
            639);
    }

    // Handler for hide login and loading panels
    function HideLoginAndLoadingPanel(funcRunAfterEffect) {
        $("#DivLogin_Body_Message").stop();
        $("#DivLogin_Body_Message").fadeOut(0);
        $("#DivLogin").animate({ "top": "100%" },
            399,
            function() {
                $("#DivLogin_BG").fadeOut(399,
                    function() {
                        $("#DivLogin_BG, #DivLogin, #DivLogin_BGColor").remove();
                        if (funcRunAfterEffect && {}.toString.call(funcRunAfterEffect) === "[object Function]") {
                            funcRunAfterEffect();
                        }
                    });
            });
    }

    // Set margin
    $("#DivLogin_Body_Message").css("margin-top", "-99px");

    // Function show error message in panel login
    function ShowErrorMessageInLogin(message) {
        $("#DivLogin_Body_Message").stop();
        $("#DivLogin_Body_Message").fadeOut(0);
        $("#DivLogin_Body_Message div").text(GetResourceLanguage(message));
        $("#DivLogin_Body_Message").animate({ "margin-top": "0px" }, { "duration": 209, "queue": false });
        $("#DivLogin_Body_Message").fadeIn(169,
            function() {
                setTimeout(function() {
                        $("#DivLogin_Body_Message")
                            .animate({ "margin-top": "-99px" }, { "duration": 369, "queue": false });
                        $("#DivLogin_Body_Message").fadeOut(339);
                    },
                    2639);

            });
    }


    // #################################### Dashboard Handler ####################################

    // ############ Handle filter by type ############

    function GetBSClassByLogType(logType) {
        switch (logType) {
        case 0:
            return "btn-success";
        case 1:
            return "btn-info";
        case 2:
            return "btn-warning";
        case 3:
            return "btn-danger";
        case 4:
            return "btn-danger";
        default:
            return "btn-secondary";
        }
    }

    $(".ShowLogFilter").click(function() {
        var logType = parseInt($(this).attr("LogType"));
        HandleShowLogFilters(logType, true);
    });

    var handlerCounter = 0;
    var _timeOutHandlerForTypeFilter;

    function HandleShowLogFilters(logType, enableToggle, forceEnable) {
        var enableLogsArray = _enableLogTypeArray;
        var selector = $(`.ShowLogFilter[LogType='${logType}']`);

        if (enableToggle)
            enableLogsArray[logType] = !enableLogsArray[logType];

        if (forceEnable)
            enableLogsArray[logType] = true;

        var specifyClass = GetBSClassByLogType(logType);
        if (enableLogsArray[logType]) {
            $(selector).removeClass("btn-dark");
            $(selector).addClass(specifyClass);
        } else {
            $(selector).addClass("btn-dark");
            $(selector).removeClass(specifyClass);
        }

        CheckFilterTypeActive();


        if (enableToggle || forceEnable) {
            clearTimeout(_timeOutHandlerForTypeFilter);
            _timeOutHandlerForTypeFilter = setTimeout(function() {
                    SetFilterOnG9DataLog(null, enableLogsArray);
                },
                99);
        }
    }

    function EnableAllShowLogFilters() {
        for (handlerCounter = 0; handlerCounter < 5; handlerCounter++) HandleShowLogFilters(handlerCounter, null, true);
    }

    function CheckFilterTypeActive() {
        for (let i = 0; i < _enableLogTypeArray.length; i++) {
            if (!_enableLogTypeArray[i]) {
                $(".ActiveFiltersDiv[type='filterType']").fadeIn(369);
                return;
            }
        }
    }

    for (handlerCounter = 0; handlerCounter < 5; handlerCounter++) HandleShowLogFilters(handlerCounter);

    // ############# Handle filter by text seach #############

    var _setTimeOutForSearch;

    var _timeOutForSeachChange = 963;

    $(".ShowTextFilter").click(function() {
        var logItemNumber = parseInt($(this).attr("LogItem"));
        ShowLoadingItems();
        HandleShowTextFilter(logItemNumber, true, null);
        HideLoadingItems();
    });


    $("#ShowTextFilter_SeachEnter").keyup(function() {
        var selector = $(this);
        clearTimeout(_setTimeOutForSearch);
        ShowLoadingItems();
        if ($(selector).val().length > 0) {
            $(".ActiveFiltersDiv[type='FilterText']").fadeIn(369);
            _setTimeOutForSearch = setTimeout(function() {
                    HandleShowTextFilter(null, true, $(selector).val());
                    HideLoadingItems();
                },
                _timeOutForSeachChange);
        } else {
            _setTimeOutForSearch = setTimeout(function() {
                    HandleShowTextFilter(null, true, "");
                    HideLoadingItems();
                },
                _timeOutForSeachChange);
            $(".ActiveFiltersDiv[type='FilterText']").fadeOut(369);
        }
    });

    function HandleShowTextFilter(logItemNumber, enableToggle, searchText) {
        var enableLogItemsSearch = _enableLogItemForSearch;
        var selector;
        if (logItemNumber || logItemNumber === 0) {
            selector = $(`.ShowTextFilter[LogItem='${logItemNumber}']`);
            if (enableToggle)
                enableLogItemsSearch[logItemNumber] = !enableLogItemsSearch[logItemNumber];

            if (enableLogItemsSearch[logItemNumber]) {
                $(selector).removeClass("btn-dark");
                $(selector).addClass("btn-primary");
            } else {
                $(selector).addClass("btn-dark");
                $(selector).removeClass("btn-primary");
            }
        }

        if (enableToggle)
            SetFilterOnG9DataLog(null, null, enableLogItemsSearch, searchText);
    }

    for (handlerCounter = 0; handlerCounter < 6; handlerCounter++) HandleShowTextFilter(handlerCounter, null, null);


    //###################################### Handle Active Filters ######################################

    $(".ActiveFiltersDiv").fadeOut();

    $(".ActiveFilter_RemoveIcon").click(function() {
        var parent = $(this).parent();
        var typeOfFilter = $(parent).attr("type");
        ClearAllFilter(typeOfFilter);
        $(parent).fadeOut(369);
    });

    function ClearAllFilter(typeOfFilter) {
        switch (typeOfFilter) {
        case "filteTime":
            $("#ContentShowLog_CustomScrollDateInner").slider("values", MinAndMaxHourAndMinutesTime);
            setTimeout(function() {
                    // Set filter
                    SetFilterOnG9DataLog([
                        ConvertMinutesToHourAndMinutes(MinAndMaxHourAndMinutesTime[0], true),
                        ConvertMinutesToHourAndMinutes(MinAndMaxHourAndMinutesTime[1], true)
                    ]);
                },
                199);
            break;
        case "filterType":
            EnableAllShowLogFilters();
            break;
        case "FilterText":
            $("#ShowTextFilter_SeachEnter").val("");
            $("#ShowTextFilter_SeachEnter").keyup();
            break;
        case "All":
            $("#ContentShowLog_CustomScrollDateInner").slider("values", MinAndMaxHourAndMinutesTime);
            setTimeout(function() {
                    // Set filter
                    SetFilterOnG9DataLog([
                        ConvertMinutesToHourAndMinutes(MinAndMaxHourAndMinutesTime[0], true),
                        ConvertMinutesToHourAndMinutes(MinAndMaxHourAndMinutesTime[1], true)
                    ]);
                },
                199);
            EnableAllShowLogFilters();
            $("#ShowTextFilter_SeachEnter").val("");
            $("#ShowTextFilter_SeachEnter").keyup();
            break;
        }
    }


    //############################################# Menu Handler #############################################
    // Field for save active current menu
    var _specifyItemMenuActive = -2;

    // Handler click for item menu
    $(".GeneralItemMenu").click(function() {
        // Switch menu
        SwitchMenuByItemMenuNumber($(this).attr("itemmenu"));
    });

    var _initializeShowAllLogsCount = false;

    // Handler for switch between menu
    function SwitchMenuByItemMenuNumber(itemMenuNumber) {

        if (!_initializeShowAllLogsCount) {
            _initializeShowAllLogsCount = true;
            setTimeout(function() {
                    // Set total logs
                    $("#TotalItemDiv_TotalLogValue").text(G9DataLog.length);
                    AnimateLogCounter($("#TotalItemDiv_TotalLogValue"));
                },
                169);
        }

        // Parse to int
        itemMenuNumber = parseInt(itemMenuNumber);

        // Return if equal with current item menu
        if (_specifyItemMenuActive === itemMenuNumber || _specifyItemMenuActive === -1) return;

        // Wait for change page
        _specifyItemMenuActive = -1;

        // Set menu color
        $("[itemmenu]").css("color", "").css("font-weight", "");
        $(`[itemmenu='${itemMenuNumber}']`).css("color", "rgb(190, 255, 0)").css("font-weight", "bold");

        switch (itemMenuNumber) {
        case 0:
            HandleHideLoginPanelAndShowDashboardPanel(true);
            $(".ChartArea").fadeOut(0);
            setTimeout(function() {
                    // Set log count
                    $("#CountEvents").text(CountEvent);
                    $("#CountInfo").text(CountInfo);
                    $("#CountWarning").text(CountWarning);
                    $("#CountError").text(CountError);
                    $("#CountException").text(CountException);
                    ShowLogCounter();
                },
                309);
            setTimeout(function() {
                    $(".ChartArea").fadeIn(169);
                    ResetAllChart();
                },
                639);
            break;
        case 1:
            HandleShowLogDataFilesGrid();
            break;
        case 999:
            location.reload();
            break;
        }
    }

    // Enable All Tooltip
    setTimeout(function() {
            $("[data-placement]").tooltip("enable");
        },
        963);
});

// Handler enter key for inputs
function HandleEnterKey(e) {
    // See notes about 'which' and 'key'
    if (e.keyCode === 13)
        $("#Btn_Login").click();
}

// Add pad right to string
function PadRight(pad, user_str) {
    if (typeof user_str === "undefined")
        return pad;
    return (user_str + pad).substring(0, pad.length);
}

// Add pad left to string
function PadLeft(pad, user_str) {
    if (typeof user_str === "undefined")
        return pad;
    return (pad + user_str).slice(-pad.length);
}

// Change number format for show #,###,###...
function NumberFormatForShow(number, n, x) {
    var re = `\\d(?=(\\d{${x || 3}})+${n > 0 ? "\\." : "$"})`;
    return number.toFixed(Math.max(0, ~~n)).replace(new RegExp(re, "g"), "$&,");
}