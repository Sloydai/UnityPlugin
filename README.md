# Sloyd AI - Generate 3D - Plugin for Unity  

**Customizable models made by humans. Assembled and stylized by AI.**  

---  

## Overview  
The **Sloyd AI Plugin for Unity** allows users to generate 3D objects from simple text prompts and iteratively edit them with additional text commands. Sloyd stands out by producing game-ready 3D models with clean topology in just seconds. These models are composed of separate parts, making them easy to modify for your specific needs.  

### Features  
- **Text-based 3D Generation**: Create 3D objects quickly using a simple text description.  
- **Iterative Editing**: Refine objects through intuitive text commands.  
- **Game-Ready Models**: Generated models feature clean topology and are ready for immediate use in your game.  
- **Modular Design**: Objects are created from separate parts, ensuring ease of customization.  

---  

## Setup  
1. Open **Packadge Manager** in your Unity project and add https://github.com/Sloydai/UnityPlugin.git
2. This package will also automatically install **gltFast Plugin** if it's not already in your project. If there's an issue you can install it manually with package manager: com.unity.cloud.gltfast 

---  

## Activation  
1. Sign up at [https://app.sloyd.ai](https://app.sloyd.ai).  
2. Subscribe to the **Plus** plan.  
3. Navigate to the **User Page** or **API Page** and generate an API key.  

---  

## How to Use  
1. **Open the Sloyd Plugin**:  
   - In Unity, navigate to **Tools** and select **Sloyd Plugin**.  

2. **Enter Your Credentials**:  
   - Paste the **Client ID** and **Client Secret** obtained from the Sloyd web app.  

3. **Generate a 3D Object**:  
   - In the **AI Prompt** field, provide a short text description of the object you want to generate.  
   - If Sloyd can generate it, the object will spawn in your Unity scene.  

4. **Edit Objects with AI**:  
   - In the **Hierarchy** tab, click on the root level of the created object.  
   - In the **Inspector**, find the **Edit with AI** text field.  
   - Type a simple text request to modify the object (e.g., change dimensions, add/remove details).  
   - If the change is successful, the modified object will replace the old one in the scene.  
   - Both the new and old versions will be saved in the **Asset Folder** for reference.  

5. **Using Sloyd in Playmode**
   - You can also expose Sloyd's AI prompting and editing in playmode and have players of your game or applicaiton create with Sloyd. 
---  
## Additional Information

### Read More
Read more about this plugin and other Sloyd products at **https://www.sloyd.ai/documentation**

### Bug Reports  
If you encounter any issues, please report them to **[dev@sloyd.ai](mailto:dev@sloyd.ai)**.  
