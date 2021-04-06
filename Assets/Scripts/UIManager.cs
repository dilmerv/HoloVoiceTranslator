using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour {

    public TMP_Dropdown LanguageList1;
    public TMP_Dropdown LanguageList2;
    public TMP_Dropdown LanguageList3;

    // Use this for initialization
    void Start () {

        List<string> languages = new List<string>();
        foreach (TranslationLanguages language in Enum.GetValues(typeof(TranslationLanguages)))
        {
            languages.Add(language.ToString());
        }
        LanguageList1.AddOptions(languages);
        LanguageList2.AddOptions(languages);
        LanguageList3.AddOptions(languages);
        // Pick some default languages for translation, users can change this
        LanguageList1.value = (int)TranslationLanguages.fr_French;
        LanguageList2.value = (int)TranslationLanguages.es_Spanish;
        LanguageList3.value = (int)TranslationLanguages.de_German;
    }

    // The first two letters of each enum value converted to a string are used
    // as translation targets, e.g. en, zh, ar, fr, nl, etc.
    // Important: THIS IS NOT THE FULL LIST OF SUPPORTED LANGUAGES. Check the
    // Languages Support page in the docs for the full list of 60+ languages at
    // https://docs.microsoft.com/azure/cognitive-services/speech-service/language-support
    public enum TranslationLanguages
    {
        ar_Arabic,
        zh_Chinese_Mandarin,
        nl_Dutch,
        en_English,
        fr_French,
        de_German,
        it_Italian,
        ja_Japanese,
        ko_Korean,
        pt_Portuguese_Brazilian,
        ru_Russian,
        es_Spanish,
        sv_Swedish,
        tlh_Klingon    // Yes, this is serious, try it
    }
}
