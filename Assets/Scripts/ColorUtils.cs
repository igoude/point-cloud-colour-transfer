using UnityEngine;

public static class ColorUtils {

    public static Color RGBtoXYZ(Color RGB) {
        // sRGB companding
        for(int i=0; i<3; i++) {
            if(RGB[i] > 0.04045f) {
                RGB[i] = Mathf.Pow((RGB[i] + 0.055f) / 1.055f, 2.4f);
            } else {
                RGB[i] /= 12.92f;
            }
        }

        // Linear rgb to XYZ
        float x = RGB[0] * 0.4124f + RGB[1] * 0.3576f + RGB[2] * 0.1805f;
        float y = RGB[0] * 0.2126f + RGB[1] * 0.7152f + RGB[2] * 0.0722f;
        float z = RGB[0] * 0.0193f + RGB[1] * 0.1192f + RGB[2] * 0.9505f;
        x = Mathf.Clamp01(x);
        y = Mathf.Clamp01(y);
        z = Mathf.Clamp01(z);

        return new Color(x, y, z);
    }

    public static Color XYZtoRGB(Color XYZ) {
        // XYZ to linear rgb
        float r = XYZ[0] * 3.2405f + XYZ[1] * (-1.5371f) + XYZ[2] * (-0.4985f);
        float g = XYZ[0] * (-0.9693f) + XYZ[1] * 1.8760f + XYZ[2] * 0.04156f;
        float b = XYZ[0] * 0.0556f + XYZ[1] * (-0.2040f) + XYZ[2] * 1.0572f;
        Color RGB = new Color(r, g, b);

        // sRGB companding
        for (int i = 0; i < 3; i++) {
            if (RGB[i] <= 0.0031308f) {
                RGB[i] *= 12.92f;
            } else {
                RGB[i] = Mathf.Pow(1.055f*RGB[i], 1.0f/2.4f) - 0.055f;
            }
        }
        RGB.r = Mathf.Clamp01(RGB.r);
        RGB.g = Mathf.Clamp01(RGB.g);
        RGB.b = Mathf.Clamp01(RGB.b);

        return RGB;
    }

    public static Color XYZtoLab(Color XYZ) {
        Color refWhite = new Color(0.95047f, 1.0f, 1.08883f); //D65
        float epsilon = 0.008856f;
        float eta = 903.3f;

        float xr = XYZ.r / refWhite.r;
        float yr = XYZ.g / refWhite.g;
        float zr = XYZ.b / refWhite.b;

        float fx = (xr > epsilon) ? Mathf.Pow(xr, 1.0f / 3.0f) : ((eta * xr) + 16.0f) / 116.0f;
        float fy = (yr > epsilon) ? Mathf.Pow(yr, 1.0f / 3.0f) : ((eta * yr) + 16.0f) / 116.0f;
        float fz = (zr > epsilon) ? Mathf.Pow(zr, 1.0f / 3.0f) : ((eta * zr) + 16.0f) / 116.0f;

        float L = 116.0f * fy - 16.0f;
        float a = 500.0f * (fx - fy);
        float b = 200.0f * (fy - fz);

        return new Color(L, a, b);
    }

    public static Color LabtoXYZ(Color Lab) {
        Color refWhite = new Color(0.95047f, 1.0f, 1.08883f); //D65
        float epsilon = 0.008856f;
        float eta = 903.3f;

        float fy = (Lab.r + 16.0f) / 116.0f;
        float fz = fy - (Lab.b / 200.0f);
        float fx = (Lab.g / 500.0f) + fy;

        float xr = (Mathf.Pow(fx, 3.0f) > epsilon) ? Mathf.Pow(fx, 3.0f) : ((116.0f * fx) - 16.0f) / eta;
        float yr = (Lab.r > eta * epsilon) ? Mathf.Pow((Lab.r + 16.0f) / 116.0f, 3.0f) : Lab.r / eta;
        float zr = (Mathf.Pow(fz, 3.0f) > epsilon) ? Mathf.Pow(fz, 3.0f) : ((116.0f * fz) - 16.0f) / eta;

        float X = xr * refWhite.r;
        float Y = yr * refWhite.g;
        float Z = zr * refWhite.b;
        X = Mathf.Clamp01(X);
        Y = Mathf.Clamp01(Y);
        Z = Mathf.Clamp01(Z);

        return new Color(X, Y, Z);
    }

    public static Color LabtoLCH(Color Lab) {
        float L = Lab.r;
        float C = Mathf.Sqrt((Lab.g * Lab.g) + (Lab.b * Lab.b));
        float H = Mathf.Atan2(Lab.b, Lab.g);

        return new Color(L, C, H * Mathf.Rad2Deg);
    }

    public static Color LCHtoLab(Color LCH) {
        float L = LCH.r;
        float a = LCH.g * Mathf.Cos(LCH.b * Mathf.Deg2Rad);
        float b = LCH.g * Mathf.Sin(LCH.b * Mathf.Deg2Rad);

        return new Color(L, a, b);
    }

    public static Color RGBtoLab(Color RGB) {
        return XYZtoLab(RGBtoXYZ(RGB));
    }

    public static Color LabtoRGB(Color Lab) {
        return XYZtoRGB(LabtoXYZ(Lab));
    }

    public static Color RGBtoLCH(Color RGB) {
        return LabtoLCH(RGBtoLab(RGB));
    }

    public static Color LCHtoRGB(Color LCH) {
        return LabtoRGB(LCHtoLab(LCH));
    }

    public static float DeltaE(Color Lab1, Color Lab2) {
        // Acceptability 2:1, Perceptibility 1:1
        float l = 1.0f; float c = 1.0f;

        float C1 = Mathf.Sqrt((Lab1.g * Lab1.g) + (Lab1.b * Lab1.b));
        float C2 = Mathf.Sqrt((Lab2.g * Lab2.g) + (Lab2.b * Lab2.b));
        float deltaC = C1 - C2;

        float deltaL = Lab1.r - Lab2.r;
        float deltaa = Lab1.g - Lab2.g;
        float deltab = Lab1.b - Lab2.b;

        float deltaH = Mathf.Sqrt((deltaa * deltaa) + (deltab * deltab) - (deltaC * deltaC));
        float H = Mathf.Atan2(Lab1.b, Lab1.g) * Mathf.Rad2Deg;

        float F = Mathf.Sqrt(Mathf.Pow(C1, 4.0f) / (Mathf.Pow(C1, 4.0f) + 1900.0f));
        float T = (164 <= H && H <= 345) ? 0.56f + Mathf.Abs(0.2f * Mathf.Cos(H + 168)) : 0.36f + Mathf.Abs(0.4f * Mathf.Cos(H + 35));

        float SL = (Lab1.r < 16) ? 0.511f : ((0.040975f * Lab1.r) / (1.0f + (0.01765f * Lab1.r)));
        float SC = ((0.0638f * C1) / (1.0f + (0.0131f * C1))) + 0.638f;
        float SH = SC * ((F * T) + 1.0f - F);

        float Ex = deltaL / l * SL;
        float Ey = deltaC / c * SC;
        float Ez = deltaH / SH;

        return Mathf.Sqrt((Ex * Ex) + (Ey * Ey) + (Ez * Ez));
    }

    public static Color RGBtoHSL(Color RGB) {
        float H = 0.0f, S = 0.0f, L = 0.0f;
        float min = Mathf.Min(RGB.r, RGB.g, RGB.b);
        float max = Mathf.Max(RGB.r, RGB.g, RGB.b);

        L = (min + max) / 2.0f;

        if (min == max) return new Color(0.0f, 0.0f, L);
        S = (L < 0.5f) ? (max - min) / (max + min) : (max - min) / (2.0f - max - min);

        if (max == RGB.r) H = (RGB.g - RGB.b) / (max - min);
        if (max == RGB.g) H = 2.0f + (RGB.b - RGB.r) / (max - min);
        if (max == RGB.b) H = 4.0f + (RGB.r - RGB.g) / (max - min);
        H *= 60.0f; if (H < 0.0f) H += 360.0f;

        return new Color(H, S, L);
    }
    
    public static Color HSLtoRGB(Color HSL) {
        float R = 0.0f, G = 0.0f, B = 0.0f;

        if (HSL.g == 0.0f) return new Color(HSL.b, HSL.b, HSL.b);

        float tempValue1 = (HSL.b < 0.5f) ? HSL.b * (1.0f + HSL.g) : HSL.b + HSL.g - (HSL.b * HSL.g);
        float tempValue2 = 2.0f * HSL.b - tempValue1;
        float hue = HSL.r / 360.0f;

        float tempR = hue + 0.333f;
        float tempG = hue;
        float tempB = hue - 0.333f;

        if (tempR < 0.0f) tempR += 1.0f; if (tempR > 1.0f) tempR -= 1.0f; 
        if (tempG < 0.0f) tempG += 1.0f; if (tempG > 1.0f) tempG -= 1.0f;
        if (tempB < 0.0f) tempB += 1.0f; if (tempB > 1.0f) tempB -= 1.0f;

        if (6.0f * tempR < 1.0f) R = tempValue2 + (tempValue1 - tempValue2) * 6.0f * tempR;
        else if (2.0f * tempR < 1.0f) R = tempValue1;
        else if (3.0f * tempR < 2.0f) R = tempValue2 + (tempValue1 - tempValue2) * 6.0f * (0.666f * tempR);
        else R = tempValue2;

        if (6.0f * tempG < 1.0f) G = tempValue2 + (tempValue1 - tempValue2) * 6.0f * tempG;
        else if (2.0f * tempG < 1.0f) G = tempValue1;
        else if (3.0f * tempG < 2.0f) G = tempValue2 + (tempValue1 - tempValue2) * 6.0f * (0.666f * tempG);
        else G = tempValue2;

        if (6.0f * tempB < 1.0f) B = tempValue2 + (tempValue1 - tempValue2) * 6.0f * tempB;
        else if (2.0f * tempB < 1.0f) B = tempValue1;
        else if (3.0f * tempB < 2.0f) B = tempValue2 + (tempValue1 - tempValue2) * 6.0f * (0.666f * tempB);
        else B = tempValue2;

        return new Color(R, G, B);
    }

    public static Color ReinhardRGBtoLab(Color RGB) {
        // RGB to LMS
        Color LMS = Color.black;
        LMS[0] = RGB[0] * 0.3811f + RGB[1] * 0.5783f + RGB[2] * 0.0402f;
        LMS[1] = RGB[0] * 0.1967f + RGB[1] * 0.7244f + RGB[2] * 0.0782f;
        LMS[2] = RGB[0] * 0.0241f + RGB[1] * 0.1288f + RGB[2] * 0.8444f;

        LMS[0] = Mathf.Log10(LMS[0] + 0.000001f);
        LMS[1] = Mathf.Log10(LMS[1] + 0.000001f);
        LMS[2] = Mathf.Log10(LMS[2] + 0.000001f);

        // LMS to Lab
        Color Lab = Color.black;
        Lab[0] = LMS[0] + LMS[1] + LMS[2];
        Lab[1] = LMS[0] + LMS[1] - 2.0f * LMS[2];
        Lab[2] = LMS[0] - LMS[1];

        Lab[0] = (1.0f / Mathf.Sqrt(3.0f)) * Lab[0];
        Lab[1] = (1.0f / Mathf.Sqrt(6.0f)) * Lab[1];
        Lab[2] = (1.0f / Mathf.Sqrt(2.0f)) * Lab[2];

        return Lab;
    }

    public static Color ReinhardLabtoRGB(Color Lab) {
        // Lab to LMS
        float l = (Mathf.Sqrt(3.0f) / 3.0f) * Lab[0];
        float a = (Mathf.Sqrt(6.0f) / 6.0f) * Lab[1];
        float b = (Mathf.Sqrt(2.0f) / 2.0f) * Lab[2];

        Color LMS = Color.black;
        LMS[0] = l + a + b;
        LMS[1] = l + a - b;
        LMS[2] = l - 2.0f * a;

        LMS[0] = Mathf.Pow(10.0f, LMS[0]);
        LMS[1] = Mathf.Pow(10.0f, LMS[1]);
        LMS[2] = Mathf.Pow(10.0f, LMS[2]);

        // LMS to RGB
        Color RGB = Color.black;
        RGB[0] = LMS[0] * 4.4678f - LMS[1] * 3.5873f + LMS[2] * 0.1193f;
        RGB[1] = -LMS[0] * 1.2186f + LMS[1] * 2.3809f - LMS[2] * 0.1624f;
        RGB[2] = LMS[0] * 0.0497f - LMS[1] * 0.2439f + LMS[2] * 1.2045f;
        
        return RGB;
    }
}
