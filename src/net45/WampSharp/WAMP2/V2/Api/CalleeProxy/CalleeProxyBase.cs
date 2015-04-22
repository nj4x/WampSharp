using System;
using System.Reflection;
using System.Threading.Tasks;
using WampSharp.V2.Client;

namespace WampSharp.V2.CalleeProxy
{
    public class CalleeProxyBase
    {
        private readonly WampCalleeProxyInvocationHandler mHandler;
        private readonly ICalleeProxyInterceptor mInterceptor;

        public CalleeProxyBase(IWampChannel channel, ICalleeProxyInterceptor interceptor)
        {
            mInterceptor = interceptor;
            IWampRealmProxy realmProxy = channel.RealmProxy;
            mHandler = new ClientInvocationHandler(realmProxy.RpcCatalog, realmProxy.Monitor);
        }

        protected T SingleInvokeSync<T>(MethodBase method, params object[] arguments)
        {
            return InvokeSync(method, new SingleValueExtractor<T>(true), arguments);
        }

        protected void SingleInvokeSync(MethodBase method, params object[] arguments)
        {
            InvokeSync(method, new SingleValueExtractor<object>(false), arguments);
        }

        protected T[] MultiInvokeSync<T>(MethodBase method, params object[] arguments)
        {
            return InvokeSync(method, new MultiValueExtractor<T>(), arguments);
        }

        protected Task<T> SingleInvokeAsync<T>(MethodBase method, params object[] arguments)
        {
            return InvokeAsync(method, new SingleValueExtractor<T>(true), arguments);
        }

        protected Task SingleInvokeAsync(MethodBase method, params object[] arguments)
        {
            return InvokeAsync(method, new SingleValueExtractor<object>(false), arguments);
        }

        protected Task<T[]> MultiInvokeAsync<T>(MethodBase method, params object[] arguments)
        {
            return InvokeAsync(method, new MultiValueExtractor<T>(), arguments);
        }

        protected Task<T> SingleInvokeProgressiveAsync<T>(MethodBase method, IProgress<T> progress,
                                                          params object[] arguments)
        {
            return InvokeProgressiveAsync<T>(method, progress, new SingleValueExtractor<T>(true), arguments);
        }

        protected Task<T[]> MultiInvokeProgressiveAsync<T>(MethodBase method, IProgress<T[]> progress,
                                                           params object[] arguments)
        {
            return InvokeProgressiveAsync<T[]>(method, progress, new MultiValueExtractor<T>(), arguments);
        }

        private T InvokeSync<T>(MethodBase method,
                                IOperationResultExtractor<T> valueExtractor,
                                params object[] arguments)
        {
            MethodInfo methodInfo = (MethodInfo) method;

            return mHandler.Invoke
                (mInterceptor,
                 methodInfo,
                 valueExtractor,
                 arguments);
        }

        private Task<T> InvokeAsync<T>(MethodBase method,
                                       IOperationResultExtractor<T> valueExtractor,
                                       params object[] arguments)
        {
            MethodInfo methodInfo = (MethodInfo) method;

            return mHandler.InvokeAsync
                (mInterceptor,
                 methodInfo,
                 valueExtractor,
                 arguments);
        }

        private Task<T> InvokeProgressiveAsync<T>
            (MethodBase method,
             IProgress<T> progress,
             IOperationResultExtractor<T> resultExtractor,
             params object[] arguments)
        {
            MethodInfo methodInfo = (MethodInfo) method;

            return mHandler.InvokeProgressiveAsync
                (mInterceptor,
                 methodInfo,
                 resultExtractor,
                 arguments,
                 progress);
        }
    }
}