using Vanara.PInvoke;
using static Vanara.PInvoke.SensorsApi;

internal class CAmbientLightAwareSensorManagerEvents : ISensorManagerEvents, IDisposable
{
	// Sensor Events class used for event sinking
	private readonly CAmbientLightAwareSensorEvents m_pSensorEvents;
	// Map to store sensors for life of class
	private readonly Dictionary<Guid, ISensor> m_Sensors = [];
	// Global to keep reference for life of class
	private ISensorManager? m_spISensorManager;

	///////////////////////////////////////////////////////////////////////////////
	// CAmbientLightAwareSensorManagerEvents::CAmbientLightAwareSensorManagerEvents
	//
	// Description of function/method: Constructor.
	//
	// Parameters: ref CAmbientLightAwareDlg dlg: pointer to parent dialog for callbacks
	//
	// Return Values: None
	///////////////////////////////////////////////////////////////////////////////
	public CAmbientLightAwareSensorManagerEvents(CAmbientLightAwareDlg dlg) => m_pSensorEvents = new(dlg, this);

	///////////////////////////////////////////////////////////////////////////////
	//
	// CAmbientLightAwareSensorManagerEvents::~CAmbientLightAwareSensorManagerEvents
	//
	// Description of function/method:
	//        Destructor. Clean up stored data.
	//
	// Parameters:
	//        none
	//
	// Return Values:
	//        None
	//
	///////////////////////////////////////////////////////////////////////////////
	public void Dispose() => m_pSensorEvents?.Dispose();

	///////////////////////////////////////////////////////////////////////////////
	// CAmbientLightAwareSensorManagerEvents::Initialize
	//
	// Description of function/method: Initialize the sensor data.
	//
	// Parameters: none
	//
	// Return Values: HRESULT.S_OK on success
	///////////////////////////////////////////////////////////////////////////////
	public HRESULT Initialize()
	{
		HRESULT hr = 0;

		try
		{
			m_spISensorManager = new();
			m_spISensorManager.SetEventSink(this);

			// Find all Ambient Light Sensors
			ISensorCollection spSensors = m_spISensorManager.GetSensorsByType(Sensors.SENSOR_TYPE_AMBIENT_LIGHT);
			uint ulCount = spSensors.GetCount();
			for (uint i = 0; i < ulCount; i++)
			{
				try
				{
					ISensor spSensor = spSensors.GetAt(i);
					hr = AddSensor(spSensor);
					if (hr.Succeeded)
					{
						hr = m_pSensorEvents.GetSensorData(spSensor);
					}
				}
				catch (Exception ex) { hr = ex.HResult; }
			}
		}
		catch (Exception ex) { hr = ex.HResult; }

		return hr;
	}

	///////////////////////////////////////////////////////////////////////////////
	// CAmbientLightAwareSensorManagerEvents::OnSensorEnter
	//
	// Description of function/method: Implementation of ISensorManager.OnSensorEnter. Check if the sensor is an Ambient Light Sensor and if
	// so then add the sensor.
	//
	// Parameters: ISensor pSensor: Sensor that has been installed SensorState state: State of the sensor
	//
	// Return Values: HRESULT.S_OK on success, else an error.
	///////////////////////////////////////////////////////////////////////////////
	HRESULT ISensorManagerEvents.OnSensorEnter(ISensor? pSensor, SensorState state)
	{
		HRESULT hr = HRESULT.S_OK;

		if (default != pSensor)
		{
			Guid idType = pSensor.GetType();
			if (idType.Equals(Sensors.SENSOR_TYPE_AMBIENT_LIGHT))
			{
				hr = AddSensor(pSensor);
				if (hr.Succeeded && SensorState.SENSOR_STATE_READY == state)
				{
					hr = m_pSensorEvents.GetSensorData(pSensor);
				}
			}
		}
		else
		{
			hr = HRESULT.E_POINTER;
		}

		return hr;
	}

	///////////////////////////////////////////////////////////////////////////////
	// CAmbientLightAwareSensorManagerEvents::RemoveSensor
	//
	// Description of function/method: Helper function, clears the event sink for the sensor and then releases the sensor.
	//
	// Parameters: REFSENSOR_ID sensorID: Input sensor
	//
	// Return Values: HRESULT.S_OK on success, else an error
	///////////////////////////////////////////////////////////////////////////////
	public HRESULT RemoveSensor(in Guid sensorID) =>
		m_Sensors.Remove(sensorID) ? HRESULT.S_OK : HRESULT.HRESULT_FROM_WIN32(Win32Error.ERROR_NOT_FOUND);

	///////////////////////////////////////////////////////////////////////////////
	// CAmbientLightAwareSensorManagerEvents::Uninitialize
	//
	// Description of function/method: Uninitialize the sensor data.
	//
	// Parameters: none
	//
	// Return Values: HRESULT.S_OK on success
	///////////////////////////////////////////////////////////////////////////////
	public HRESULT Uninitialize()
	{
		HRESULT hr = HRESULT.S_OK;

		foreach (var pSensor in m_Sensors.Values.ToArray())
		{
			RemoveSensor(pSensor);
		}

		if (m_spISensorManager is not null)
		{
			try { m_spISensorManager.SetEventSink(default); }
			catch (Exception ex) { hr = ex.HResult; }
		}

		return hr;
	}

	///////////////////////////////////////////////////////////////////////////////
	// CAmbientLightAwareSensorManagerEvents::AddSensor
	//
	// Description of function/method: Helper function, sets up event sinking for a sensor and saves the sensor.
	//
	// Parameters: ISensor pSensor: Input sensor
	//
	// Return Values: HRESULT.S_OK on success, else an error
	///////////////////////////////////////////////////////////////////////////////
	private HRESULT AddSensor(ISensor pSensor)
	{
		try
		{
			pSensor.SetEventSink(m_pSensorEvents);
			// Get the sensor's ID to be used as a key to store the sensor
			Guid idSensor = pSensor.GetID();
			// Enter the sensor into the map and take the ownership of its lifetime
			m_Sensors[idSensor] = pSensor;
			return HRESULT.S_OK;
		}
		catch (Exception ex) { return ex.HResult; }
	}

	///////////////////////////////////////////////////////////////////////////////
	// CAmbientLightAwareSensorManagerEvents::RemoveSensor
	//
	// Description of function/method: Helper function, clears the event sink for the sensor and then releases the sensor.
	//
	// Parameters: ISensor pSensor: Input sensor
	//
	// Return Values: HRESULT.S_OK on success, else an error
	///////////////////////////////////////////////////////////////////////////////
	private HRESULT RemoveSensor(ISensor pSensor)
	{
		try
		{
			// Release the event and ISensor objecets
			pSensor.SetEventSink(null); // This also decreases the ref count of the sink object.

			Guid idSensor = pSensor.GetID();
			m_Sensors.Remove(idSensor);

			Marshal.ReleaseComObject(pSensor);
			return HRESULT.S_OK;
		}
		catch (Exception ex) { return ex.HResult; }
	}
}