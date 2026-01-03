:- include('findactions').
:- initialization(main).

main :- getRecommendedActions(standard,[driving,routePlanned,exitClear,headlightsOff,roadAheadClear,sidelightsOff,bendInRoad,fuel,turning,dualCarriageWay,canReadNumberPlate,allPassengersWearingSeatBeltsAsRequired,vehicleSafe,completeOvertakeBeforeSolidWhiteLine,vehicleDoesntFitsInCentralReservation,allChildrenUsingChildSeatAsRequired],[],Actions), write(Actions), halt(0).