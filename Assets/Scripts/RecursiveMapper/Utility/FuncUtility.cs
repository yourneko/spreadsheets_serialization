using System;

namespace RecursiveMapper.Utility
{
    static class FuncUtility
    {
        public static Func<TValue, TBind> Bind<TValue, TResult, TBind>(this Func<TValue, TResult> f, Func<TResult, TBind> g)
        {
            TBind fxg(TValue value) => g (f (value));
            return fxg;
        }

        public static Action<TValue> Bind<TValue, TResult>(this Func<TValue, TResult> f, Action<TResult> a)
        {
            void fxa(TValue value) => a (f (value));
            return fxa;
        }
    }
}