# Summary
This spec describes the current settings for the controller, as displayed in the official FarDriver phone app for tuning the controller.  Individual settings have been transcribed from the the screenshots of the phone, with english labels, their current value, and known details about the expectations about the settings ought to be display in the UI.  Enums should be a dropdown menu, allowing the user to pick from a fixed set of values.  Some values have very clear units, but very few settings have information about the limits on the range of the value.  I have no idea what some of the values are or what they do, and we should not implement code to change them unless we can clearly determine their purpose from the C++ far driver controller project, or I update this spec.


# Settings Sections

## Motor Parameters

Motor Sensor Type: 2-  (label in FarDriver app is 'AngleDetect' and I believe this is an enum for the type of sensor used to detect the motor's rotation)
Phase Offset: 30 (unsure, label taken directly from FarDriver app)
Motor Direction 0  (Enum: 0 = Clockwise, 1 = CounterClockwise)
Temperature Sensor: 6 (Enum: 6 = NTC10K, other values TBD)
Pole Pairs: 4  (unsigned integer, range TBD)
Rated Voltage: 72V (unsigned integer, range TBD)
Rated RPM: 6800rpm (unsigned integer, range TBD)
Reverse RPM: 1000rpm (unsigned integer, range TBD)
Max RPM: 9000rpm  (unsigned integer, range TBD)
Max Line Current: 180A (unsigned integer, range TBD)
Max Phase Current: 450A (unsigned integer, range TBD)
Boost Line Current: 250A  (unsigned integer, range TBD)
Boose Phase Current: 550A  (unsigned integer, range TBD)
Phase Exchange: No Exchange (no idea what this is - enum? TBD)
Field Weakening: 0 (Enum, 0=Fast, other values TBD)
Weak Response: 0 (no idea what this is, likely an integer value)
Throttle Response: 0 (Enum: 0 = Line, other values TBD)
Throttle Acceleration Step: 224 (unsigned integer, range TBD)
Throttle Deceleration Step: 224 (unsigned integer, range TBD)
Release Throttle: 0 (no idea what this is)
Throttle Closed Voltage: 0.9V  (float, upper threshold voltage for throttle to be completely off)
Throttle Fully Open Voltage: 3.75V  (float, lower threshold for throttle to be fully open)


## Power Curve

There's a mapping of RPM to a percentage value described as "Ratios in Speed" - but it's unclear what the percentage represents. Ideally we'd represent this as a graph with RPM on the X-axis, and percentage on the Y-axis  Current mapping values are:
500RPM 75%
1000RPM 75%
1500RPM 75%
2000RPM 75%
2500rpm 75%
3000rpm 75%
3500rpm 75%
4000rpm 75%
4500rpm 75%
5000rpm 73%
5500rpm 71%
6000rpm 69%
6500rpm 67%
7000rpm 65%
7500rpm 63%
8000rpm 61%
8500rpm 61%
9000rpm 15%

Other values in the 'Ratios in Speed' section
LD: 900  (unsure, label taken directly from FarDriver app)
LQ: 329 (unsure, label taken directly from FarDriver app)
FAIF: 513 (unsure, label taken directly from FarDriver app)
RPM Limit: 9000RPM  (motor rpm limit?  Label is LimitSpeed in FarDriver app)


## Regen Settings

Similar to the power curve settings, aka "Ratios in Speed", regen contains a mappingof RPMs to percentage values.  We should be able to share a similar UI representation to the Power Curve, but showing the amount of regen (essentially engine braking) being applied at each rpm
500RPM -13%
1000RPM -16%
1500RPM -19%
2000RPM -22%
2500rpm -25%
3000rpm -25%
3500rpm -25%
4000rpm -25%
4500rpm -25%
5000rpm -25%
5500rpm -25%
6000rpm -25%
6500rpm -25%
7000rpm -25%
7500rpm -25%
8000rpm -25%
8500rpm -25%
9000rpm 0%

Other simple settings related to regen are:
Stop Back Current: 25A (integer, need to determine valid range)
Max Back Current: 30A (integer, need to determine valid range)
Battery Rated Capacity: 55AH  (integer. should this even be editable? Can we read it from the BMS?)
Free Throttle: 0  (not sure if this is a boolean, enum, or integer)
Brake Voltage: 0.31V  (I think this is the threshold value from the brake switch signal to indicate the rider is braking, and determines when braking regen is applied)

## Drive Mode Settings  
These are from the 'Ratios in Gear' section in the FarDriver app.  It appears 'gear' is a bad translation of 'mode'.  I suspect 'LowSpeed' indicates settings for mode '2' on the dash, and 'MiddleSpeed' correlates to mode '3', and drive mode '4' uses 100% of the line current, phase current, and motor speed.  I don't know what drives the settings for mode '1' (aka, Eco).
Mode 1 Line Current Percent: 40%  (LowSpeedLineRatio in FarDriver.  Appears to specify a percentage of Max Line Current setting for Mode 1)
Mode 1 Phase Percent: 60%  (LowSpeedPhaseRatio in FarDriver.  Appears to specify a percentage of Max Phase Current setting for Mode 1)
Mode 1 Max Motor Speed: 5000rpm
Mode 2 Line Current Percent: 70% (MidSpeedLineRatio in FarDriver app)
Mode 2 Phase Percent: 75% (MidSpeedPhaseRatio in FarDriver app)
Mode 2 Max Motor Speed: 7000rpm

## Feature Settings

Will iterate on this in future planning sessions

## Display Settings

Will iterate on this in future planning sessions

## Safety Settings

Will iterate on this in future planning sessions

## PID Parameters

Will iterate on this in future planning sessions

## Read Only Parameters

Will iterate on this in future planning sessions
