using Scte35.Net.Constants;

namespace Scte35.Net.Model.Enums;

public enum SpliceCommandType : byte
{
	SpliceNull = Scte35Constants.SpliceNullCommand,
	SpliceSchedule = Scte35Constants.SpliceScheduleCommand,
	SpliceInsert = Scte35Constants.SpliceInsertCommand,
	TimeSignal = Scte35Constants.TimeSignalCommand,
	BandwidthReservation = Scte35Constants.BandwidthReservationCommand,
	PrivateCommand = Scte35Constants.PrivateCommand,
}
