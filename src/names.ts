import names from "../config/names.json";

class ITraderNames {
    Prapor?: string;
    Therapist?: string;
    Fence?: string;
    Skier?: string;
    Peacekeeper?: string;
    Mechanic?: string;
    Ragman?: string;
    Jaeger?: string;
    Lightkeeper?: string;
    [key: string]: string;
}

export const NAMES: ITraderNames = names;
