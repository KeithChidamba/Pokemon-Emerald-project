using System.Collections.Generic;
public static class Utility
{
    public static string removeSpace(string name_)
    {
        char splitter = ' ';
        int space_count = 0;
        List<int> num_spaces = new();
        for (int i = 0; i < name_.Length; i++)
        {
            if (name_[i] == splitter)
            {
                num_spaces.Add(i);
                space_count++;
            }
        }
        string result = "";
        if (space_count > 0)
        {
            int index = 0;
            for (int i = 0; i < space_count; i++)
            {
                result += name_.Substring(index,(num_spaces[i]-index));
                index = num_spaces[i]+1;
            }
            //part after last space
            result+=name_.Substring(num_spaces[space_count - 1]+1, (name_.Length - num_spaces[space_count - 1]-1));
        }
        else
        {
            result = name_;
        }
        return result;
    }
}
