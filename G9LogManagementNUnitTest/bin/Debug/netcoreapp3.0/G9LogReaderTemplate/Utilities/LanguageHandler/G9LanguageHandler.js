// Create custom event for change language
function OnStarterLanguage(languageInformation) {
    const customEventStarterLanguage = new CustomEvent("onStarterLanguage", { detail: languageInformation });
    window.dispatchEvent(customEventStarterLanguage);
}

// Create custom event for change language
function OnChangeLanguage(languageInformation) {
    const customEventChangeLanguage = new CustomEvent("onChangeLanguage", { detail: languageInformation });
    window.dispatchEvent(customEventChangeLanguage);
}

// Fill 'ResourceLang' with language file
// Language Directory `Utilities/LanguageHandler/Lang/Lang-` + cultureName + `.js?v${Math.random()}`
// Language file name = 'Lang-{cultureName}.js' => Lang.en-us.js Or Lang.fa.js
// G9ResourceLanguage {
//  "string cultureName" :
//      { "string key": "string value", ... }
// }
// G9RL <=> G9ResourceLanguage
var G9ResourceLanguage = {};

// Set default culture
var defaultCulture = DefaultCulture ? DefaultCulture : "en-us";

// If culture not found => set default culture
if (typeof LanguageConfigItems[defaultCulture] === "undefined") {
    defaultCulture = "en-us";
}

// Specify all language script loaded
var readyAllLanguageScriptLoads = false;

// Add all language dictionary
function AddLanguage(callBackFuncLoadSuccess) {
    try {
        var countOfLanguageScript = 0;
        var countOfLanguageConfigItems = 0;
        // Add another cultures
        $.each(LanguageConfigItems,
            function(index, value) {
                countOfLanguageConfigItems++;
                const lastLanguageScript = document.createElement("script");
                lastLanguageScript.onload = function() {
                    countOfLanguageScript++;
                    if (countOfLanguageScript === countOfLanguageConfigItems) {
                        HandlerChangeLanguage();
                        if (isFunction(callBackFuncLoadSuccess)) {
                            callBackFuncLoadSuccess();
                        }
                    }
                };
                lastLanguageScript.src = `Utilities/LanguageHandler/Lang/Lang-${index}${`.js?v${Math.random()}`}`;
                document.getElementsByTagName("head")[0].appendChild(lastLanguageScript);
            });

    } catch (error) {
        console.log(error);
    }
}

// Get Resource language by key
// Param key => key in resource
// Param customParamForCulture => if need get key value from custom culture set customParamForCulture
function GetResourceLanguage(key, customParamForCulture, debugModeForConsole) {
    var result = key;
    cultureForUse = customParamForCulture ? customParamForCulture : defaultCulture;
    if (G9ResourceLanguage && G9ResourceLanguage[cultureForUse]) {
        // if enable debug mode
        if (debugModeForConsole) console.log(`########## Start Search For ${key} ##########`);
        // Each items and find value of key
        $.each(G9ResourceLanguage[cultureForUse],
            function(index, value) {
                // if enable debug mode
                if (debugModeForConsole)
                    console.log(index.toUpperCase() +
                        " == " +
                        key.toUpperCase() +
                        " => " +
                        (index.toUpperCase() === key.toUpperCase()));
                // if find value of key => set result and break
                if (index.toUpperCase() === key.toUpperCase()) {
                    result = value;
                    return false;
                }
            });
        return result;
    } else {
        return result;
    }
}

// Check object has property
// if has property return true
function HasOwnProperty(obj, prop) {
    const proto = obj.__proto__ || obj.constructor.prototype;
    return (prop in obj) &&
        (!(prop in proto) || proto[prop] !== obj[prop]);
}

// Convert gregorian date time to culture date time
function ConvertDateTimeWithCulture(gregorianDateTime) {
    // Check all validation and exists method convert
    if (LanguageConfigItems &&
        LanguageConfigItems[cultureForUse] &&
        HasOwnProperty(LanguageConfigItems[cultureForUse], "CultureDateTimeConvertor") &&
        LanguageConfigItems[cultureForUse].CultureDateTimeConvertor &&
        {}.toString.call(LanguageConfigItems[cultureForUse].CultureDateTimeConvertor) === "[object Function]") {
        // Convert to culture date time
        return LanguageConfigItems[cultureForUse].CultureDateTimeConvertor(gregorianDateTime);
    } else {
        // if validation is false return date time
        return gregorianDateTime;
    }
}

// Handle change language => translate all fields
function HandlerChangeLanguage() {
    setTimeout(function() {
            // Change language image
            $(".G9-Language-Image").attr("src", LanguageConfigItems[defaultCulture].Image);

            // Bind value
            // <... g9-bind-value="resourceKey" ...
            $("[g9-bind-value]").each(function(index) {
                const varG9BindValue = $(this).attr("g9-bind-value");
                if (varG9BindValue && varG9BindValue.length > 0) {
                    if ($(this).is("input"))
                        $(this).val(GetResourceLanguage(varG9BindValue));
                    else
                        $(this).html(GetResourceLanguage(varG9BindValue));
                }
            });

            // Bind attribute
            // <... g9-bind-attr="attrName:resourceKey" ...
            $("[g9-bind-attr]").each(function(index) {
                const varG9BindAttr = $(this).attr("g9-bind-attr");
                if (varG9BindAttr && varG9BindAttr.length > 0) {
                    const splitG9Attr = varG9BindAttr.split(":");
                    if (splitG9Attr.length === 2) {
                        $(this).attr(splitG9Attr[0], GetResourceLanguage(splitG9Attr[1]));
                    }
                }
            });

            // Bind Converted date time with culture
            // <... g9-culture-datetime="GregorianDateTime YYYY/MM/DD HH:mm:SS.ff" ...
            $("[g9-culture-datetime]").each(function(index) {
                const varG9BindDateTime = $(this).attr("g9-culture-datetime");
                // Minimum length => 'YYYY/MM/DD'.length === 10
                if (varG9BindDateTime && varG9BindDateTime.length >= 10) {
                    if ($(this).is("input"))
                        $(this).val(ConvertDateTimeWithCulture(varG9BindDateTime));
                    else
                        $(this).html(ConvertDateTimeWithCulture(varG9BindDateTime));
                }
            });

        },
        169);
}

// Check variable is function
// return true if is function
function isFunction(functionToCheck) {
    return functionToCheck && {}.toString.call(functionToCheck) === "[object Function]";
}

// Add language selector tag for change language
function AddLanguageForChoose() {
    if (LanguageConfigItems) {

        // Add another cultures
        $.each(LanguageConfigItems,
            function(index, value) {
                $(".G9-Language-LanguageItems").append(`
                        <div G9ChangeLanguage="${index}" G9LanguageDirection="${value.Direction
                    }" class="G9-Language-LanguageItems_innerDiv">
                            <div class="G9-Language-DivImg-Items">
                                <img class="G9-Language-Image-choose" src="${value.Image}" />
                            </div>
                            <div class="G9-Language-DivLabel-Items">
                                <lable class="G9-Language-Label">${GetResourceLanguage("Language", index)}</lable>
                                <lable class="G9-Language-Label"> - </lable>
                                <lable class="G9-Language-Label">${GetResourceLanguage("CultureFullLanguageName",
                        index)}</lable>
                            </div>
                        </div>
                    `);
            });
    } else {
        throw new exception("Error initialize 'LanguageConfigItems'!");
    }
}

// Handle event for choose new language
$(document).on("click",
    "[G9ChangeLanguage]",
    function() {
        // If culture equal with previous culture => return
        if (defaultCulture === $(this).attr("G9ChangeLanguage"))
            return;

        // Change culture and translate
        defaultCulture = $(this).attr("G9ChangeLanguage");
        HandlerChangeLanguage();

        // Dispatch the event 'OnChangeLanguage'
        OnChangeLanguage(LanguageConfigItems[defaultCulture]);
    });

// Add all language script specify in Languages.js
AddLanguage(function() {
    readyAllLanguageScriptLoads = true;
});

// After ready document => start translate
$(document).ready(function() {

    function ChangeLanguageIfAllLanguageScriptLoad() {
        if (readyAllLanguageScriptLoads) {
            AddLanguageForChoose();
            setTimeout(function() {
                    // Dispatch the event 'OnStarterLanguage'
                    OnStarterLanguage(LanguageConfigItems[defaultCulture]);
                },
                369);
        } else {
            setTimeout(ChangeLanguageIfAllLanguageScriptLoad, 9);
        }
    }

    ChangeLanguageIfAllLanguageScriptLoad();
});