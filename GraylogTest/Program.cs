using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace GraylogTest
{
    public static class Program
    {
        private static readonly ContextualLogger Logger = new ContextualLogger(LogManager.GetCurrentClassLogger());
        public static void Main(string[] args)
        {
            var r = new Random();
            
            for (var i = 0; i < 50; i++)
            {
                using (new LogContext()
                    .With("CustomerNumber", r.Next(0, 10).ToString())
                    .With("UserId", r.Next(0, 5).ToString()))
                {
                    Logger.Debug("Hello World");
                }
            }
        }
    }

    #region Contextual logging boilerplate (HACK)
    public class ContextualLogger
    {
        private readonly Logger _logger;

        public ContextualLogger(Logger logger)
        {
            _logger = logger;
        }

        public void Debug(string message, params object[] parameters)
        {
            var eventInfo = LogEventInfo.Create(LogLevel.Debug, _logger.Name, null, message, parameters);
            foreach (var entry in LogContext.Values)
            {
                eventInfo.Properties.Add(entry.Key, entry.Value);
            }
            _logger.Log(typeof(ContextualLogger), eventInfo);
        }
    }
    public class LogContext : IDisposable
    {
        [ThreadStatic]
        private static Stack<Dictionary<string, string>> _dictionaries;

        public LogContext()
        {
            if (_dictionaries == null)
            {
                _dictionaries = new Stack<Dictionary<string, string>>();
            }

            _dictionaries.Push(new Dictionary<string, string>());
        }

        public static IEnumerable<KeyValuePair<string, string>> Values
        {
            get
            {
                return _dictionaries == null || _dictionaries.Count == 0
                    ? new KeyValuePair<string, string>[0]
                    : _dictionaries.SelectMany(keyValuePairs => keyValuePairs)
                        .Distinct(
                            new LambdaComparer<KeyValuePair<string, string>>((kvpA, kvpB) => kvpA.Key.Equals(kvpB.Key)));
            }
        }

        public void Dispose()
        {
            _dictionaries.Pop();
            if (_dictionaries.Count == 0)
            {
                _dictionaries = null;
            }
        }

        public LogContext With<T>(string key, T value)
        {
            _dictionaries.Peek().Add(key, value.ToString());
            return this;
        }
    }
    public class LambdaComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _lambdaComparer;
        private readonly Func<T, int> _lambdaHash;

        public LambdaComparer(Func<T, T, bool> lambdaComparer) :
            this(lambdaComparer, o => 0)
        { }

        private LambdaComparer(Func<T, T, bool> lambdaComparer, Func<T, int> lambdaHash)
        {
            _lambdaComparer = lambdaComparer;
            _lambdaHash = lambdaHash;
        }

        public bool Equals(T x, T y)
        {
            return _lambdaComparer(x, y);
        }

        public int GetHashCode(T obj)
        {
            return _lambdaHash(obj);
        }
    } 
    #endregion
}
