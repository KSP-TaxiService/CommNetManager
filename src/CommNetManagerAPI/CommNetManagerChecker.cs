﻿using System;
using System.Collections.Generic;
using System.Linq;
using CommNet;

namespace CommNetManagerAPI
{
    /// <summary>
    /// Static API class to facilitate checking if CommNetManager is installed.
    /// </summary>
    /// <remarks>
    /// Use <see cref="CommNetManagerInstalled"/> to determine if CommNetManager is installed.<para />
    /// Use <see cref="SetCommNetManagerIfAvailable(CommNetScenario)"/> to activate CommNetManager if it is installed.<para />
    /// Use <see cref="GetCommNetManagerInstance"/> to get the handle to the current CommNetManagerNetwork instance.<para />
    /// </remarks>
    public static class CommNetManagerChecker
    {
        /// <summary>
        /// Gets the current <see cref="CommNet.CommNetNetwork"/> instance.
        /// </summary>
        public static CommNetNetwork CommNetNetwork
        {
            get
            {
                if(_commNetwork == null)
                {
                        _commNetwork = CommNetScenario.FindObjectOfType<CommNetNetwork>();
                }
                return _commNetwork;
            }
        }
        private static CommNetNetwork _commNetwork = null;
        /// <summary>
        /// Checks if CommNetManager is installed.
        /// </summary>
        /// <returns>True if CommNetManager is installed.</returns>
        /// <remarks>
        /// Result is cached to minimize Reflection calls.
        /// </remarks>
        public static bool CommNetManagerInstalled
        {
            get
            {
                if (CommNetManagerChecked == false)
                {
                    UnityEngine.Debug.Log("Checking for CommNetManager...");
                    System.Reflection.Assembly CNMAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly =>
                        assembly.FullName.StartsWith("CommNetManager") && !assembly.FullName.StartsWith("CommNetManagerAPI"));
                    CommNetManager = CNMAssembly != null ? CNMAssembly.GetType("CommNetManager.CommNetManager", true) : null;
                    _CommNetManagerInstalled = !(CommNetManager == null);
                    if (_CommNetManagerInstalled)
                    {
                        CommNetExtensions.commNodesVessels =
                            (Dictionary<CommNode, Vessel>)
                            CNMAssembly.GetType("CommNetManager.CommNetManagerNetwork", true)
                            .GetField("commNodesVessels", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                            .GetValue(null);
                        CommNetExtensions.getVessel = CommNetExtensions.GetVesselCNM;
                    }
                    CommNetManagerChecked = true;
                    UnityEngine.Debug.Log("CommNetManager " + (_CommNetManagerInstalled ? "is" : "is not") + " installed.");
                }
                return _CommNetManagerInstalled;
            }
        }
        private static bool _CommNetManagerInstalled;
        private static bool CommNetManagerChecked = false;
        private static Type CommNetManager = null;
        private static System.Reflection.PropertyInfo CommNetManagerInstance_prop = null;
        private static System.Reflection.PropertyInfo CommNetManagerNetwork_prop = null;
        private static System.Reflection.MethodInfo BindTo_method = null;

        /// <summary>
        /// Gets the current instance of the CommNetManager. Can be cast to <see cref="ICommNetManager"/>.
        /// </summary>
        /// <returns>Null if CommNetManager is not installed.</returns>
        public static CommNetNetwork GetCommNetManagerInstance()
        {
            if (CommNetManagerInstalled)
            {
                if (CommNetManagerInstance_prop == null)
                {
                    CommNetManagerInstance_prop = CommNetManager.GetProperty("Instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly);
                }
                return (CommNetNetwork)CommNetManagerInstance_prop.GetValue(null, null);
            }
            else
                return null;
        }

        /// <summary>
        /// Gets the current instance of the CommNet. Can be cast to <see cref="IPublicCommNet"/>.
        /// </summary>
        /// <returns>Null if CommNetManager is not installed.</returns>
        public static CommNetwork GetCommNetManagerNetwork()
        {
            if (CommNetManagerInstalled)
            {
                if (CommNetManagerNetwork_prop == null)
                {
                    CommNetManagerNetwork_prop = CommNetManager.GetProperty("CommNet", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                }
                return (CommNetwork)CommNetManagerNetwork_prop.GetValue(GetCommNetManagerInstance(), null);
            }
            else
                return null;
        }

        /// <summary>
        /// Binds the provided CommNetwork to the current CommNetManagerNetwork instance.
        /// </summary>
        /// <param name="bind">The derived CommNetwork instance which should be bound to CommNetManagerNetwork.</param>
        /// <returns>True if succesful at binding, false if not.</returns>
        /// <remarks>
        /// This method links the protected fields inherited from CommNet.Network.Net<para />
        /// <list type="bullet">
        /// <listheader><description>CAUTION: When bound to CommNetManager, you should ensure the following methods do not call base.method():<para /></description></listheader>
        /// <item><description>Add</description></item>
        /// <item><description>Remove</description></item>
        /// <item><description>Connect</description></item>
        /// <item><description>Disconnect</description></item>
        /// <item><description>Rebuild</description></item>
        /// </list>
        /// </remarks>
        public static bool BindToCommNetManager(CommNetwork bind)
        {
            CommNetwork CommNetManagerInstance = GetCommNetManagerNetwork();
            if (CommNetManagerInstance == null)
                return false;

            if (BindTo_method == null)
                BindTo_method = CommNetManager.GetMethod("BindNetwork", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            return (bool)BindTo_method.Invoke(CommNetManagerInstance, new object[] { bind });
        }

        /// <summary>
        /// Sets the CommNetNetwork object to be CommNetManager if CommNetManager is installed or a supplied type if it is not.
        /// </summary>
        /// <param name="scenario">Your instance of CommNetScenario.</param>
        /// <param name="derivativeOfCommNetNetwork">Type of your network to instantiate if CommNetManager is not installed.</param>
        /// <param name="CustomCommNetNetwork">The handle of the CommNetNetwork object that is being implemented.</param>
        /// <returns>True if CommNetManager or the supplied Net was instantiated, false if not.</returns>
        public static bool SetCommNetManagerIfAvailable(this CommNetScenario scenario, Type derivativeOfCommNetNetwork, out CommNetNetwork CustomCommNetNetwork)
        {
            CustomCommNetNetwork = null;
            //Replace the CommNet network
            CommNetNetwork stockNet = CommNetScenario.FindObjectOfType<CommNetNetwork>();
            
            if (CommNetManagerInstalled)
            {
                if (stockNet.GetType() == CommNetManager)
                {
                    CustomCommNetNetwork = stockNet;
                    return true;
                }
                CustomCommNetNetwork = (CommNetNetwork)scenario.gameObject.AddComponent(CommNetManager);
            }
            else
            {
                if (!typeof(CommNetNetwork).IsAssignableFrom(derivativeOfCommNetNetwork))
                {
                    UnityEngine.Debug.LogError("The supplied Type in SetCommNetManagerIfAvailable is not a derivative of CommNetNetwork.");
                    return false;
                }
                CustomCommNetNetwork = (CommNetNetwork)scenario.gameObject.AddComponent(derivativeOfCommNetNetwork);
            }

            if (CustomCommNetNetwork != null)
            {
                UnityEngine.Object.Destroy(stockNet);
                _commNetwork = CustomCommNetNetwork;
                //CommNetNetwork.Instance.GetType().GetMethod("set_Instance").Invoke(CustomCommNetNetwork, null); // reflection to bypass Instance's protected set // don't seem to work
                return true;
            }
            else
            {
                CustomCommNetNetwork = stockNet;
                _commNetwork = stockNet;
                return false;
            }
        }
        /// <summary>
        /// Sets the CommNetNetwork object to be CommNetManager if CommNetManager is installed or a supplied type if it is not.
        /// </summary>
        /// <param name="scenario">Your instance of CommNetScenario.</param>
        /// <param name="derivativeOfCommNetNetwork">Type of your network to instantiate if CommNetManager is not installed.</param>
        /// <returns>True if CommNetManager or the supplied Net was instantiated, False if not.</returns>
        public static bool SetCommNetManagerIfAvailable(this CommNetScenario scenario, Type derivativeOfCommNetNetwork)
        {
            return SetCommNetManagerIfAvailable(scenario, derivativeOfCommNetNetwork, out _);
        }

        /// <summary>
        /// Sets the CommNetNetwork object to be CommNetManager if CommNetManager is installed.
        /// </summary>
        /// <param name="scenario">Your instance of CommNetScenario.</param>
        /// <param name="CustomCommNetNetwork">The handle of the CommNetNetwork object that is being implemented.</param>
        /// <returns>True if CommNetManager or the supplied Net was instantiated, False if not.</returns>
        public static bool SetCommNetManagerIfAvailable(this CommNetScenario scenario, out CommNetNetwork CustomCommNetNetwork)
        {
            CustomCommNetNetwork = null;
            //Replace the CommNet network
            CommNetNetwork stockNet = UnityEngine.Object.FindObjectOfType<CommNetNetwork>();

            if (CommNetManagerInstalled)
            {
                if (stockNet.GetType() == CommNetManager)
                {
                    CustomCommNetNetwork = stockNet;
                    return true;
                }
                CustomCommNetNetwork = (CommNetNetwork)scenario.gameObject.AddComponent(CommNetManager);
            }
            else
                return false;

            if (!(CustomCommNetNetwork == null))
            {
                UnityEngine.Object.Destroy(stockNet);
                _commNetwork = CustomCommNetNetwork;
                //CommNetNetwork.Instance.GetType().GetMethod("set_Instance").Invoke(CustomCommNetNetwork, null); // reflection to bypass Instance's protected set // don't seem to work
                return true;
            }
            else
            {
                CustomCommNetNetwork = stockNet;
                _commNetwork = stockNet;
                return false;
            }
        }
        /// <summary>
        /// Sets the CommNetNetwork object to be CommNetManager if CommNetManager is installed.
        /// </summary>
        /// <param name="scenario">Your instance of CommNetScenario.</param>
        /// <returns>True if CommNetManager or the supplied Net was instantiated, False if not.</returns>
        public static bool SetCommNetManagerIfAvailable(this CommNetScenario scenario)
        {
            return SetCommNetManagerIfAvailable(scenario, out _);
        }
    }
}
