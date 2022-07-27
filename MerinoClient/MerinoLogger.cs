using System;
#if DEBUG
using MelonLoader;
#endif

namespace MerinoClient
{
    internal static class MerinoLogger
    {
#if DEBUG
        private static readonly MelonLogger.Instance Instance = new("Merino[DEV]", ConsoleColor.White);
#endif

        #region Msg

        public static void Msg(object obj)
        {
#if DEBUG
            Instance.Msg(obj);
#else
            MerinoLoader.MerinoLogger.Msg(obj);
#endif
        }

        public static void Msg(string txt)
        {
#if DEBUG
            Instance.Msg(txt);
#else
            MerinoLoader.MerinoLogger.Msg(txt);
#endif
        }

        public static void Msg(string txt, params object[] args)
        {
#if DEBUG
            Instance.Msg(txt, args);
#else
            MerinoLoader.MerinoLogger.Msg(txt, args);
#endif
        }

        public static void Msg(ConsoleColor txtColor, object obj)
        {
#if DEBUG
            Instance.Msg(txtColor, obj);
#else
            MerinoLoader.MerinoLogger.Msg(txtColor, obj);
#endif
        }

        public static void Msg(ConsoleColor txtColor, string txt)
        {
#if DEBUG
            Instance.Msg(txtColor, txt);
#else
            MerinoLoader.MerinoLogger.Msg(txtColor, txt);
#endif
        }

        public static void Msg(ConsoleColor txtColor, string txt, params object[] args)
        {
#if DEBUG
            Instance.Msg(txtColor, txt, args);
#else
            MerinoLoader.MerinoLogger.Msg(txtColor, txt, args);
#endif
        }

        #endregion

        #region Warning

        public static void Warning(object obj)
        {
#if DEBUG
            Instance.Warning(obj);
#else
            MerinoLoader.MerinoLogger.Warning(obj);
#endif
        }

        public static void Warning(string txt)
        {
#if DEBUG
            Instance.Warning(txt);
#else
            MerinoLoader.MerinoLogger.Warning(txt);
#endif
        }

        public static void Warning(string txt, params object[] args)
        {
#if DEBUG
            Instance.Warning(txt, args);
#else
            MerinoLoader.MerinoLogger.Warning(txt, args);
#endif
        }

        #endregion

        #region Error

        public static void Error(object obj)
        {
#if DEBUG
            Instance.Error(obj);
#else
            MerinoLoader.MerinoLogger.Error(obj);
#endif
        }

        public static void Error(string txt)
        {
#if DEBUG
            Instance.Error(txt);
#else
            MerinoLoader.MerinoLogger.Error(txt);
#endif
        }

        public static void Error(string txt, params object[] args)
        {
#if DEBUG
            Instance.Error(txt, args);
#else
            MerinoLoader.MerinoLogger.Error(txt, args);
#endif
        }

        public static void Error(string txt, Exception ex)
        {
#if DEBUG
            Instance.Error(txt, ex);
#else
            MerinoLoader.MerinoLogger.Error(txt, ex);
#endif
        }

        #endregion
    }
}