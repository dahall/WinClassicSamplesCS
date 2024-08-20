using static Vanara.PInvoke.User32;

//
// assume a maximum of 10 contacts and turn touch feedback off
//
InitializeTouchInjection(10, TOUCH_FEEDBACK.TOUCH_FEEDBACK_NONE);

//
// initialize the touch info structure
//
POINTER_TOUCH_INFO contact = new()
{
	touchFlags = TOUCH_FLAGS.TOUCH_FLAGS_NONE,
	touchMask = TOUCH_MASK.TOUCH_MASK_CONTACTAREA | TOUCH_MASK.TOUCH_MASK_ORIENTATION | TOUCH_MASK.TOUCH_MASK_PRESSURE,
	orientation = 90,
	pressure = 32000,
	pointerInfo = new()
	{
		pointerType = POINTER_INPUT_TYPE.PT_TOUCH, //we're sending touch input
		pointerId = 0, //contact 0
		pointerFlags = POINTER_FLAGS.POINTER_FLAG_DOWN | POINTER_FLAGS.POINTER_FLAG_INRANGE | POINTER_FLAGS.POINTER_FLAG_INCONTACT,
		ptPixelLocation = new(640, 480)
	},
	//
	// set the contact area depending on thickness
	//
	rcContact = new(640 - 2, 480 - 2, 640 + 2, 480 + 2)
};

//
// inject a touch down
//
bool bRet = InjectTouchInput(1, [contact]);

//
// if touch down was succesfull, send a touch up
//
if (bRet)
{
	contact.pointerInfo.pointerFlags = POINTER_FLAGS.POINTER_FLAG_UP;

	//
	// inject a touch up
	//
	InjectTouchInput(1, [contact]);
}