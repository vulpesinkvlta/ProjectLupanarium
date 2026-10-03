using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Code.Gameplay
{
    // Locale state belongs exclusively to Unity Localization. Tables are preloaded asynchronously for WebGL.
    public static class L10n
    {
        public const string TableName = "GameText";
        private static readonly Dictionary<string,StringTable> Tables = new();
        public static bool IsReady { get; private set; }
        [DllImport("__Internal")] private static extern string YG_Language();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { Tables.Clear(); IsReady = false; }
        public static string NormalizeLanguage(string code)
        {
            string language=(code??"").ToLowerInvariant().Split('-','_')[0];
            return language=="ru" || language=="tr" ? language : "en";
        }
        public static IEnumerator Initialize()
        {
            if(IsReady)yield break;
            string language=Application.systemLanguage==SystemLanguage.Russian ? "ru" : Application.systemLanguage==SystemLanguage.Turkish ? "tr" : "en";
#if UNITY_WEBGL && YANDEX_GAMES && !UNITY_EDITOR
            float deadline=Time.realtimeSinceStartup+15;
            string portal="";
            while(string.IsNullOrEmpty(portal) && Time.realtimeSinceStartup<deadline) { portal=YG_Language(); if(string.IsNullOrEmpty(portal))yield return null; }
            language=NormalizeLanguage(portal);
#endif
            yield return LocalizationSettings.InitializationOperation;
            foreach(string code in new[]{"ru","en","tr"})
            {
                Locale locale=LocalizationSettings.AvailableLocales.GetLocale(code);
                if(locale==null)continue;
                var operation=LocalizationSettings.StringDatabase.GetTableAsync(TableName,locale);
                yield return operation;
                if(operation.Result!=null)Tables[code]=operation.Result;
            }
            LocalizationSettings.SelectedLocale=LocalizationSettings.AvailableLocales.GetLocale(language);
            IsReady=true;
        }
        public static string Text(string key)
        {
            if(string.IsNullOrEmpty(key))return key??"";
            if(!IsReady)return key; // Editor authoring and non-bootstrapped unit tests retain source text.
            string code=NormalizeLanguage(LocalizationSettings.SelectedLocale.Identifier.Code);
            if(Tables.TryGetValue(code,out var table)) { var entry=table.GetEntry(key); if(entry!=null && !string.IsNullOrEmpty(entry.Value))return entry.Value; }
            if(Tables.TryGetValue("en",out table)) { var entry=table.GetEntry(key); if(entry!=null && !string.IsNullOrEmpty(entry.Value))return entry.Value; }
            return key;
        }
        public static string F(FormattableString value) => Format(value.Format,value.GetArguments());
        public static string Format(string key,params object[] arguments)
        {
            var args=(object[])arguments.Clone();
            for(int i=0;i<args.Length;i++) if(args[i] is string text)args[i]=Text(text);
            var culture=IsReady ? LocalizationSettings.SelectedLocale.Identifier.CultureInfo : CultureInfo.CurrentCulture;
            return string.Format(culture,Text(key),args);
        }
    }
}
