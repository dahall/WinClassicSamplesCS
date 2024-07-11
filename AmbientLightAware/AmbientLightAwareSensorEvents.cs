using Vanara.PInvoke;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.SensorsApi;

internal class CAmbientLightAwareSensorEvents : ISensorEvents, IDisposable
{
	private readonly Dictionary<Guid, float> m_mapLux = [];

	// Parent dialog used for callbacks
	private readonly CAmbientLightAwareDlg m_pParentDlg;

	// Parent class for callbacks Map to store lux values for each sensor
	private readonly CAmbientLightAwareSensorManagerEvents m_pSensorManagerEvents;

	///////////////////////////////////////////////////////////////////////////////
	// CAmbientLightAwareSensorEvents::CAmbientLightAwareSensorEvents
	//
	// Description of function/method: Constructor.
	//
	// Parameters: ref CAmbientLightAwareDlg dlg: Parent dialog for callbacks ref CAmbientLightAwareSensorManagerEvents sensorManagerEvents:
	// Parent class for callbacks
	//
	// Return Values: None
	///////////////////////////////////////////////////////////////////////////////
	public CAmbientLightAwareSensorEvents(CAmbientLightAwareDlg dlg, CAmbientLightAwareSensorManagerEvents sensorManagerEvents)
	{
		m_pParentDlg = dlg;
		m_pSensorManagerEvents = sensorManagerEvents;
	}

	///////////////////////////////////////////////////////////////////////////////
	// CAmbientLightAwareSensorEvents::GetSensorData
	//
	// Description of function/method: Helper function, get data from a sensor and updates the lux
	//
	// Parameters: ISensor pSensor: Input sensor
	//
	// Return Values: HRESULT.S_OK on success, else an error
	///////////////////////////////////////////////////////////////////////////////
	public HRESULT GetSensorData(ISensor pSensor)
	{
		if (default != pSensor)
		{
			try
			{
				ISensorDataReport spDataReport = pSensor.GetData();
				return GetSensorData(pSensor, spDataReport);
			}
			catch
			{
				Guid idSensor = pSensor.GetID();
				m_mapLux[idSensor] = -1.0f;
				return UpdateLux();
			}
		}
		else
		{
			return HRESULT.E_POINTER;
		}
	}

	///////////////////////////////////////////////////////////////////////////////
	// CAmbientLightAwareSensorEvents::~CAmbientLightAwareSensorEvents
	//
	// Description of function/method: Destructor. Clean up stored data.
	//
	// Parameters: none
	//
	// Return Values: None
	///////////////////////////////////////////////////////////////////////////////
	public void Dispose() => m_mapLux.Clear();

	///////////////////////////////////////////////////////////////////////////////
	// CAmbientLightAwareSensorEvents::OnDataUpdated
	//
	// Description of function/method: Implementation of ISensor.OnDataUpdated. Called when the sensor has published new data. This reads in
	// the lux value from the report.
	//
	// Parameters: ISensor pSensor: Sensor that has updated data. ISensorDataReport pNewData: New data to be read
	//
	// Return Values: HRESULT.S_OK on success, else an error
	///////////////////////////////////////////////////////////////////////////////
	HRESULT ISensorEvents.OnDataUpdated(ISensor? pSensor, ISensorDataReport? pNewData) =>
		default != pSensor && default != pNewData ? GetSensorData(pSensor, pNewData) : HRESULT.E_UNEXPECTED;

	///////////////////////////////////////////////////////////////////////////////
	// CAmbientLightAwareSensorEvents::OnEvent
	//
	// Description of function/method: Implementation of ISensor.OnEevent, a generic or custom sensor event. OnDataUpdated is the event this
	// sample uses to get sensor information.
	//
	// Parameters: ISensor pSensor: Sensor that has the event. Guid& eventID: Type of event. IPortableDeviceValues pEventData: Data to be read.
	//
	// Return Values: HRESULT.S_OK
	///////////////////////////////////////////////////////////////////////////////
	HRESULT ISensorEvents.OnEvent(ISensor? pSensor, in Guid eventID, PortableDeviceApi.IPortableDeviceValues? pEventData) =>
		// Not implemented
		HRESULT.E_NOTIMPL;

	///////////////////////////////////////////////////////////////////////////////
	// CAmbientLightAwareSensorEvents::OnLeave
	//
	// Description of function/method: Implementation of ISensor.OnLeave. This sensor is being removed, so it needs to be deleted and freed.
	//
	// Parameters: REFSENSOR_ID sensorID:
	//
	// Return Values: HRESULT.S_OK
	///////////////////////////////////////////////////////////////////////////////
	HRESULT ISensorEvents.OnLeave(in Guid sensorID)
	{
		HRESULT hr = m_pSensorManagerEvents.RemoveSensor(sensorID); // Callback into parent
		if (hr.Succeeded)
		{
			// Remove the data for this device
			if (m_mapLux.Remove(sensorID))
			{
				hr = UpdateLux();
			}
		}

		return hr;
	}

	///////////////////////////////////////////////////////////////////////////////
	// CAmbientLightAwareSensorEvents::OnStateChanged
	//
	// Description of function/method: Implementation of ISensor.OnStateChanged. Called when permissions of the sensor have changed, such as
	// the sensor being disabled in control panel. If the sensor is not SENSOR_STATE_READY then its lux value should be ignored.
	//
	// Parameters: ISensor pSensor: Sensor that has changed SensorState state: State of the sensor
	//
	// Return Values: HRESULT.S_OK
	///////////////////////////////////////////////////////////////////////////////
	HRESULT ISensorEvents.OnStateChanged(ISensor? pSensor, SensorState state)
	{
		if (default != pSensor)
		{
			Guid idSensor = pSensor.GetID();
			if (SensorState.SENSOR_STATE_READY == state)
			{
				return GetSensorData(pSensor);
			}
			else
			{
				// If the sensor is not ready, its lux value is ignored.
				m_mapLux[idSensor] = -1.0f;
				return UpdateLux();
			}
		}
		else
		{
			return HRESULT.E_POINTER;
		}
	}

	///////////////////////////////////////////////////////////////////////////////
	// CAmbientLightAwareSensorEvents::GetSensorData
	//
	// Description of function/method: Helper function, get data from a sensor and updates the lux
	//
	// Parameters: ISensor pSensor: Input sensor ISensorDataReport pDataReport: The data to be read
	//
	// Return Values: HRESULT.S_OK on success, else an error
	///////////////////////////////////////////////////////////////////////////////
	private HRESULT GetSensorData(ISensor pSensor, ISensorDataReport pDataReport)
	{
		if (default != pSensor && default != pDataReport)
		{
			try
			{
				Guid idSensor = pSensor.GetID();
				var pvLux = pDataReport.GetSensorValue(Sensors.SENSOR_DATA_TYPE_LIGHT_LEVEL_LUX);

				// Save the lux value into our new member variable
				m_mapLux[idSensor] = (float)pvLux!;
				return UpdateLux();
			}
			catch (Exception e) { return e.HResult; }
		}
		else
		{
			return HRESULT.E_INVALIDARG;
		}
	}

	///////////////////////////////////////////////////////////////////////////////
	// CAmbientLightAwareSensorEvents::UpdateLux
	//
	// Description of function/method: Helper function, calculates average lux and does a callback to the parent dialog.
	//
	// Parameters: none
	//
	// Return Values: none
	///////////////////////////////////////////////////////////////////////////////
	private HRESULT UpdateLux()
	{
		float fpLux = m_mapLux.Values.Average();

		return m_pParentDlg.UpdateLux(fpLux, m_mapLux.Count);
	}
}