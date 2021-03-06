using System;
using NLog;

namespace AxBatchRunner.AxWrapper
{
    /// <summary>
    ///   Proxy for run Axapta classes
    /// </summary>
    public sealed class AxProxy : IDisposable
    {
        private readonly IAxBusinessConnector _connector;
        private readonly AxSettings _settings;
        private readonly ThreadSafeFlag _isConnected = new ThreadSafeFlag();
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private AxConfig _config;

        /// <summary>
        ///   Hide default constructor
        /// </summary>
        private AxProxy()
        {
        }

        /// <summary>
        ///   Constructor
        /// </summary>
        /// <param name = "settings">AxSettings</param>
        public AxProxy(AxSettings settings)
        {
            _settings = settings;
            if (_settings.DaxVersion == 3)
            {
                _connector = new AxBusinessConnectorCom();
            }
            else
            {
                _connector = new AxBusinessConnectorNet();
            }
        }

        /// <summary>
        ///   Is BC connected to Axapta
        /// </summary>
        public bool IsConnected
        {
            get { return _isConnected.Value; }
        }

        /// <summary>
        ///   Logon to Axapta
        /// </summary>
        public void Logon()
        {
            try
            {
                Logger.Info("Trying connect to Axapta...");
                _connector.Logon(_settings.User, _settings.Password, _settings.Company, _settings.Language,
                                 _settings.Configuration);
                _isConnected.Set();
                _config = GetConfig();
                Logger.Info("Axapta connected.");
                Logger.Info("Connection info: {0}", _config);
            }
            catch (AxException exception)
            {
                _isConnected.Clear();
                Logger.Info("Failed connect to Axapta.");
                Logger.Error(exception.Message);
            }
        }

        /// <summary>
        ///   Logoff from Axapta
        /// </summary>
        public void Logoff()
        {
            _connector.Logoff();
            _isConnected.Clear();
        }

        public AxConfig GetConfig()
        {
            return new AxConfig((string) _connector.CallStatic("xInfo", "serialNo"),
                                (string) _connector.CallStatic("xInfo", "licenseName"),
                                (bool) _connector.CallStatic("xGlobal", "isAOS"),
                                (string) _connector.CallStatic(_settings.BatchRunnerClass, "getAOSName"));
        }

        /// <summary>
        ///   Run Axapta RunBaseBatch class
        /// </summary>
        /// <param name = "batchGroup">The name of  batch group</param>
        /// <param name = "cancelJobIfError">Not recuring job if error</param>
        /// <param name = "delBatchAfterSuccess"></param>
        /// <returns>Response from RunBaseBatch (infolog)</returns>
        public string StartBatch(string batchGroup, bool cancelJobIfError, bool delBatchAfterSuccess)
        {
            string outStr = string.Empty;

            try
            {
                outStr = (string) _connector.CallStatic(_settings.BatchRunnerClass,
                                                        _settings.BatchRunnerMethod,
                                                        batchGroup, cancelJobIfError,
                                                        delBatchAfterSuccess);
            }
            catch (AxException)
            {
                _isConnected.Clear();
            }

            return outStr;
        }

        /// <summary>
        ///   Run Axapta RunBaseBatch class
        /// </summary>
        /// <param name = "batchGroup">The name of  batch group</param>
        /// <returns>Response from RunBaseBatch (infolog)</returns>
        public string StartBatch(string batchGroup)
        {
            return StartBatch(batchGroup, true, true);
        }

        /// <summary>
        ///   Has more batches for run?
        /// </summary>
        /// <param name = "batchGroup">The name of  batch group</param>
        /// <returns>true - Yes, false - No</returns>
        public bool HasBatches(string batchGroup)
        {
            bool hasBatches = false;

            try
            {
                hasBatches = (bool) _connector.CallStatic(_settings.BatchRunnerClass, "hasBatches", batchGroup);
            }
            catch (AxException)
            {
                _isConnected.Clear();
            }

            return hasBatches;
        }

        /// <summary>
        ///   Implementation of the Dispose Pattern
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///   Implementation of the Dispose Pattern
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_connector != null)
                {
                    _connector.Dispose();
                }
            }
        }

        /// <summary>
        ///   Implementation of the Dispose Pattern
        /// </summary>
        ~AxProxy()
        {
            Dispose();
        }
    }
}