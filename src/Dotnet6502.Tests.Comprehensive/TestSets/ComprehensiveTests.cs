namespace Dotnet6502.Tests.Comprehensive.TestSets;

public class ComprehensiveTests
{
    // Load Instructions - LDA
    [Fact]
    public async Task Run_LDA_A1_Tests()
    {
        await TestCaseRunner.Run("a1.json");
    }

    [Fact]
    public async Task Run_LDA_A5_Tests()
    {
        await TestCaseRunner.Run("a5.json");
    }

    [Fact]
    public async Task Run_LDA_A9_Tests()
    {
        await TestCaseRunner.Run("a9.json");
    }

    [Fact]
    public async Task Run_LDA_Ad_Tests()
    {
        await TestCaseRunner.Run("ad.json");
    }

    [Fact]
    public async Task Run_LDA_B1_Tests()
    {
        await TestCaseRunner.Run("b1.json");
    }

    [Fact]
    public async Task Run_LDA_B5_Tests()
    {
        await TestCaseRunner.Run("b5.json");
    }

    [Fact]
    public async Task Run_LDA_B9_Tests()
    {
        await TestCaseRunner.Run("b9.json");
    }

    [Fact]
    public async Task Run_LDA_Bd_Tests()
    {
        await TestCaseRunner.Run("bd.json");
    }

    // Load Instructions - LDX
    [Fact]
    public async Task Run_LDX_A2_Tests()
    {
        await TestCaseRunner.Run("a2.json");
    }

    [Fact]
    public async Task Run_LDX_A6_Tests()
    {
        await TestCaseRunner.Run("a6.json");
    }

    [Fact]
    public async Task Run_LDX_Ae_Tests()
    {
        await TestCaseRunner.Run("ae.json");
    }

    [Fact]
    public async Task Run_LDX_B6_Tests()
    {
        await TestCaseRunner.Run("b6.json");
    }

    [Fact]
    public async Task Run_LDX_Be_Tests()
    {
        await TestCaseRunner.Run("be.json");
    }

    // Load Instructions - LDY
    [Fact]
    public async Task Run_LDY_A0_Tests()
    {
        await TestCaseRunner.Run("a0.json");
    }

    [Fact]
    public async Task Run_LDY_A4_Tests()
    {
        await TestCaseRunner.Run("a4.json");
    }

    [Fact]
    public async Task Run_LDY_Ac_Tests()
    {
        await TestCaseRunner.Run("ac.json");
    }

    [Fact]
    public async Task Run_LDY_B4_Tests()
    {
        await TestCaseRunner.Run("b4.json");
    }

    [Fact]
    public async Task Run_LDY_Bc_Tests()
    {
        await TestCaseRunner.Run("bc.json");
    }

    // Store Instructions - STA
    [Fact]
    public async Task Run_STA_81_Tests()
    {
        await TestCaseRunner.Run("81.json");
    }

    [Fact]
    public async Task Run_STA_85_Tests()
    {
        await TestCaseRunner.Run("85.json");
    }

    [Fact]
    public async Task Run_STA_8d_Tests()
    {
        await TestCaseRunner.Run("8d.json");
    }

    [Fact]
    public async Task Run_STA_91_Tests()
    {
        await TestCaseRunner.Run("91.json");
    }

    [Fact]
    public async Task Run_STA_95_Tests()
    {
        await TestCaseRunner.Run("95.json");
    }

    [Fact]
    public async Task Run_STA_99_Tests()
    {
        await TestCaseRunner.Run("99.json");
    }

    [Fact]
    public async Task Run_STA_9d_Tests()
    {
        await TestCaseRunner.Run("9d.json");
    }

    // Store Instructions - STX
    [Fact]
    public async Task Run_STX_86_Tests()
    {
        await TestCaseRunner.Run("86.json");
    }

    [Fact]
    public async Task Run_STX_8e_Tests()
    {
        await TestCaseRunner.Run("8e.json");
    }

    [Fact]
    public async Task Run_STX_96_Tests()
    {
        await TestCaseRunner.Run("96.json");
    }

    // Store Instructions - STY
    [Fact]
    public async Task Run_STY_84_Tests()
    {
        await TestCaseRunner.Run("84.json");
    }

    [Fact]
    public async Task Run_STY_8c_Tests()
    {
        await TestCaseRunner.Run("8c.json");
    }

    [Fact]
    public async Task Run_STY_94_Tests()
    {
        await TestCaseRunner.Run("94.json");
    }

    // Transfer Instructions
    [Fact]
    public async Task Run_TXA_8a_Tests()
    {
        await TestCaseRunner.Run("8a.json");
    }

    [Fact]
    public async Task Run_TYA_98_Tests()
    {
        await TestCaseRunner.Run("98.json");
    }

    [Fact]
    public async Task Run_TXS_9a_Tests()
    {
        await TestCaseRunner.Run("9a.json");
    }

    [Fact]
    public async Task Run_TAY_A8_Tests()
    {
        await TestCaseRunner.Run("a8.json");
    }

    [Fact]
    public async Task Run_TAX_Aa_Tests()
    {
        await TestCaseRunner.Run("aa.json");
    }

    [Fact]
    public async Task Run_TSX_Ba_Tests()
    {
        await TestCaseRunner.Run("ba.json");
    }

    // Stack Instructions
    [Fact]
    public async Task Run_PHP_08_Tests()
    {
        await TestCaseRunner.Run("08.json");
    }

    [Fact]
    public async Task Run_PLP_28_Tests()
    {
        await TestCaseRunner.Run("28.json");
    }

    [Fact]
    public async Task Run_PHA_48_Tests()
    {
        await TestCaseRunner.Run("48.json");
    }

    [Fact]
    public async Task Run_PLA_68_Tests()
    {
        await TestCaseRunner.Run("68.json");
    }

    // Logic Instructions - AND
    [Fact]
    public async Task Run_AND_21_Tests()
    {
        await TestCaseRunner.Run("21.json");
    }

    [Fact]
    public async Task Run_AND_25_Tests()
    {
        await TestCaseRunner.Run("25.json");
    }

    [Fact]
    public async Task Run_AND_29_Tests()
    {
        await TestCaseRunner.Run("29.json");
    }

    [Fact]
    public async Task Run_AND_2d_Tests()
    {
        await TestCaseRunner.Run("2d.json");
    }

    [Fact]
    public async Task Run_AND_31_Tests()
    {
        await TestCaseRunner.Run("31.json");
    }

    [Fact]
    public async Task Run_AND_35_Tests()
    {
        await TestCaseRunner.Run("35.json");
    }

    [Fact]
    public async Task Run_AND_39_Tests()
    {
        await TestCaseRunner.Run("39.json");
    }

    [Fact]
    public async Task Run_AND_3d_Tests()
    {
        await TestCaseRunner.Run("3d.json");
    }

    // Logic Instructions - EOR
    [Fact]
    public async Task Run_EOR_41_Tests()
    {
        await TestCaseRunner.Run("41.json");
    }

    [Fact]
    public async Task Run_EOR_45_Tests()
    {
        await TestCaseRunner.Run("45.json");
    }

    [Fact]
    public async Task Run_EOR_49_Tests()
    {
        await TestCaseRunner.Run("49.json");
    }

    [Fact]
    public async Task Run_EOR_4d_Tests()
    {
        await TestCaseRunner.Run("4d.json");
    }

    [Fact]
    public async Task Run_EOR_51_Tests()
    {
        await TestCaseRunner.Run("51.json");
    }

    [Fact]
    public async Task Run_EOR_55_Tests()
    {
        await TestCaseRunner.Run("55.json");
    }

    [Fact]
    public async Task Run_EOR_59_Tests()
    {
        await TestCaseRunner.Run("59.json");
    }

    [Fact]
    public async Task Run_EOR_5d_Tests()
    {
        await TestCaseRunner.Run("5d.json");
    }

    // Logic Instructions - ORA
    [Fact]
    public async Task Run_ORA_01_Tests()
    {
        await TestCaseRunner.Run("01.json");
    }

    [Fact]
    public async Task Run_ORA_05_Tests()
    {
        await TestCaseRunner.Run("05.json");
    }

    [Fact]
    public async Task Run_ORA_09_Tests()
    {
        await TestCaseRunner.Run("09.json");
    }

    [Fact]
    public async Task Run_ORA_0d_Tests()
    {
        await TestCaseRunner.Run("0d.json");
    }

    [Fact]
    public async Task Run_ORA_11_Tests()
    {
        await TestCaseRunner.Run("11.json");
    }

    [Fact]
    public async Task Run_ORA_15_Tests()
    {
        await TestCaseRunner.Run("15.json");
    }

    [Fact]
    public async Task Run_ORA_19_Tests()
    {
        await TestCaseRunner.Run("19.json");
    }

    [Fact]
    public async Task Run_ORA_1d_Tests()
    {
        await TestCaseRunner.Run("1d.json");
    }

    // Logic Instructions - BIT
    [Fact]
    public async Task Run_BIT_24_Tests()
    {
        await TestCaseRunner.Run("24.json");
    }

    [Fact]
    public async Task Run_BIT_2c_Tests()
    {
        await TestCaseRunner.Run("2c.json");
    }

    // Arithmetic Instructions - ADC
    [Fact]
    public async Task Run_ADC_61_Tests()
    {
        await TestCaseRunner.Run("61.json");
    }

    [Fact]
    public async Task Run_ADC_65_Tests()
    {
        await TestCaseRunner.Run("65.json");
    }

    [Fact]
    public async Task Run_ADC_69_Tests()
    {
        await TestCaseRunner.Run("69.json");
    }

    [Fact]
    public async Task Run_ADC_6d_Tests()
    {
        await TestCaseRunner.Run("6d.json");
    }

    [Fact]
    public async Task Run_ADC_71_Tests()
    {
        await TestCaseRunner.Run("71.json");
    }

    [Fact]
    public async Task Run_ADC_75_Tests()
    {
        await TestCaseRunner.Run("75.json");
    }

    [Fact]
    public async Task Run_ADC_79_Tests()
    {
        await TestCaseRunner.Run("79.json");
    }

    [Fact]
    public async Task Run_ADC_7d_Tests()
    {
        await TestCaseRunner.Run("7d.json");
    }

    // Arithmetic Instructions - SBC
    [Fact]
    public async Task Run_SBC_E1_Tests()
    {
        await TestCaseRunner.Run("e1.json");
    }

    [Fact]
    public async Task Run_SBC_E5_Tests()
    {
        await TestCaseRunner.Run("e5.json");
    }

    [Fact]
    public async Task Run_SBC_E9_Tests()
    {
        await TestCaseRunner.Run("e9.json");
    }

    [Fact]
    public async Task Run_SBC_Ed_Tests()
    {
        await TestCaseRunner.Run("ed.json");
    }

    [Fact]
    public async Task Run_SBC_F1_Tests()
    {
        await TestCaseRunner.Run("f1.json");
    }

    [Fact]
    public async Task Run_SBC_F5_Tests()
    {
        await TestCaseRunner.Run("f5.json");
    }

    [Fact]
    public async Task Run_SBC_F9_Tests()
    {
        await TestCaseRunner.Run("f9.json");
    }

    [Fact]
    public async Task Run_SBC_Fd_Tests()
    {
        await TestCaseRunner.Run("fd.json");
    }

    // Compare Instructions - CMP
    [Fact]
    public async Task Run_CMP_C1_Tests()
    {
        await TestCaseRunner.Run("c1.json");
    }

    [Fact]
    public async Task Run_CMP_C5_Tests()
    {
        await TestCaseRunner.Run("c5.json");
    }

    [Fact]
    public async Task Run_CMP_C9_Tests()
    {
        await TestCaseRunner.Run("c9.json");
    }

    [Fact]
    public async Task Run_CMP_Cd_Tests()
    {
        await TestCaseRunner.Run("cd.json");
    }

    [Fact]
    public async Task Run_CMP_D1_Tests()
    {
        await TestCaseRunner.Run("d1.json");
    }

    [Fact]
    public async Task Run_CMP_D5_Tests()
    {
        await TestCaseRunner.Run("d5.json");
    }

    [Fact]
    public async Task Run_CMP_D9_Tests()
    {
        await TestCaseRunner.Run("d9.json");
    }

    [Fact]
    public async Task Run_CMP_Dd_Tests()
    {
        await TestCaseRunner.Run("dd.json");
    }

    // Compare Instructions - CPX
    [Fact]
    public async Task Run_CPX_E0_Tests()
    {
        await TestCaseRunner.Run("e0.json");
    }

    [Fact]
    public async Task Run_CPX_E4_Tests()
    {
        await TestCaseRunner.Run("e4.json");
    }

    [Fact]
    public async Task Run_CPX_Ec_Tests()
    {
        await TestCaseRunner.Run("ec.json");
    }

    // Compare Instructions - CPY
    [Fact]
    public async Task Run_CPY_C0_Tests()
    {
        await TestCaseRunner.Run("c0.json");
    }

    [Fact]
    public async Task Run_CPY_C4_Tests()
    {
        await TestCaseRunner.Run("c4.json");
    }

    [Fact]
    public async Task Run_CPY_Cc_Tests()
    {
        await TestCaseRunner.Run("cc.json");
    }

    // Increment Instructions - INC
    [Fact]
    public async Task Run_INC_E6_Tests()
    {
        await TestCaseRunner.Run("e6.json");
    }

    [Fact]
    public async Task Run_INC_Ee_Tests()
    {
        await TestCaseRunner.Run("ee.json");
    }

    [Fact]
    public async Task Run_INC_F6_Tests()
    {
        await TestCaseRunner.Run("f6.json");
    }

    [Fact]
    public async Task Run_INC_Fe_Tests()
    {
        await TestCaseRunner.Run("fe.json");
    }

    // Increment Instructions - INX
    [Fact]
    public async Task Run_INX_E8_Tests()
    {
        await TestCaseRunner.Run("e8.json");
    }

    // Increment Instructions - INY
    [Fact]
    public async Task Run_INY_C8_Tests()
    {
        await TestCaseRunner.Run("c8.json");
    }

    // Decrement Instructions - DEC
    [Fact]
    public async Task Run_DEC_C6_Tests()
    {
        await TestCaseRunner.Run("c6.json");
    }

    [Fact]
    public async Task Run_DEC_Ce_Tests()
    {
        await TestCaseRunner.Run("ce.json");
    }

    [Fact]
    public async Task Run_DEC_D6_Tests()
    {
        await TestCaseRunner.Run("d6.json");
    }

    [Fact]
    public async Task Run_DEC_De_Tests()
    {
        await TestCaseRunner.Run("de.json");
    }

    // Decrement Instructions - DEX
    [Fact]
    public async Task Run_DEX_Ca_Tests()
    {
        await TestCaseRunner.Run("ca.json");
    }

    // Decrement Instructions - DEY
    [Fact]
    public async Task Run_DEY_88_Tests()
    {
        await TestCaseRunner.Run("88.json");
    }

    // Shift Instructions - ASL
    [Fact]
    public async Task Run_ASL_06_Tests()
    {
        await TestCaseRunner.Run("06.json");
    }

    [Fact]
    public async Task Run_ASL_0a_Tests()
    {
        await TestCaseRunner.Run("0a.json");
    }

    [Fact]
    public async Task Run_ASL_0e_Tests()
    {
        await TestCaseRunner.Run("0e.json");
    }

    [Fact]
    public async Task Run_ASL_16_Tests()
    {
        await TestCaseRunner.Run("16.json");
    }

    [Fact]
    public async Task Run_ASL_1e_Tests()
    {
        await TestCaseRunner.Run("1e.json");
    }

    // Shift Instructions - LSR
    [Fact]
    public async Task Run_LSR_46_Tests()
    {
        await TestCaseRunner.Run("46.json");
    }

    [Fact]
    public async Task Run_LSR_4a_Tests()
    {
        await TestCaseRunner.Run("4a.json");
    }

    [Fact]
    public async Task Run_LSR_4e_Tests()
    {
        await TestCaseRunner.Run("4e.json");
    }

    [Fact]
    public async Task Run_LSR_56_Tests()
    {
        await TestCaseRunner.Run("56.json");
    }

    [Fact]
    public async Task Run_LSR_5e_Tests()
    {
        await TestCaseRunner.Run("5e.json");
    }

    // Shift Instructions - ROL
    [Fact]
    public async Task Run_ROL_26_Tests()
    {
        await TestCaseRunner.Run("26.json");
    }

    [Fact]
    public async Task Run_ROL_2a_Tests()
    {
        await TestCaseRunner.Run("2a.json");
    }

    [Fact]
    public async Task Run_ROL_2e_Tests()
    {
        await TestCaseRunner.Run("2e.json");
    }

    [Fact]
    public async Task Run_ROL_36_Tests()
    {
        await TestCaseRunner.Run("36.json");
    }

    [Fact]
    public async Task Run_ROL_3e_Tests()
    {
        await TestCaseRunner.Run("3e.json");
    }

    // Shift Instructions - ROR
    [Fact]
    public async Task Run_ROR_66_Tests()
    {
        await TestCaseRunner.Run("66.json");
    }

    [Fact]
    public async Task Run_ROR_6a_Tests()
    {
        await TestCaseRunner.Run("6a.json");
    }

    [Fact]
    public async Task Run_ROR_6e_Tests()
    {
        await TestCaseRunner.Run("6e.json");
    }

    [Fact]
    public async Task Run_ROR_76_Tests()
    {
        await TestCaseRunner.Run("76.json");
    }

    [Fact]
    public async Task Run_ROR_7e_Tests()
    {
        await TestCaseRunner.Run("7e.json");
    }

    // Return Instructions - RTS
    [Fact]
    public async Task Run_RTS_60_Tests()
    {
        await TestCaseRunner.Run("60.json");
    }

    // Return Instructions - RTI
    [Fact]
    public async Task Run_RTI_40_Tests()
    {
        await TestCaseRunner.Run("40.json");
    }

    // Set Flag Instructions
    [Fact]
    public async Task Run_SEC_38_Tests()
    {
        await TestCaseRunner.Run("38.json");
    }

    [Fact]
    public async Task Run_SED_F8_Tests()
    {
        await TestCaseRunner.Run("f8.json");
    }

    [Fact]
    public async Task Run_SEI_78_Tests()
    {
        await TestCaseRunner.Run("78.json");
    }

    // Clear Flag Instructions
    [Fact]
    public async Task Run_CLC_18_Tests()
    {
        await TestCaseRunner.Run("18.json");
    }

    [Fact]
    public async Task Run_CLD_D8_Tests()
    {
        await TestCaseRunner.Run("d8.json");
    }

    [Fact]
    public async Task Run_CLI_58_Tests()
    {
        await TestCaseRunner.Run("58.json");
    }

    [Fact]
    public async Task Run_CLV_B8_Tests()
    {
        await TestCaseRunner.Run("b8.json");
    }

    // Other Instructions
    [Fact]
    public async Task Run_BRK_00_Tests()
    {
        await TestCaseRunner.Run("00.json");
    }

    [Fact]
    public async Task Run_NOP_Ea_Tests()
    {
        await TestCaseRunner.Run("ea.json");
    }
}
